# ライセンスクライアント認証アーキテクチャ

> **版:** v0.1（2026-04-28）
> **対象範囲:** pdfHandler / ZipSearch / PictComp の 3 製品（共通仕様）
> **目的:** Stripe 設問 **Q6-1（不正ログイン対策）**「該当なし：会員ログイン機能はありません」の根拠ドキュメント。**会員アカウント・パスワード・Web ログイン UI を一切持たない**ことを構造的に説明する。
> **対応エビデンス:** `legals/security-compliance/Security checklist for Credit/PHASE_3_1_FINALIZED_ANSWERS.md` §4.12 ／ `PHASE_3_1_TRACEABILITY_MATRIX.md` §4.12

---

## 1. 結論

**ユーザーは ID／パスワードでログインしません。**
ライセンス認証は、購入時に発行される **32 文字のライセンスキー**と、**端末固有のハードウェア ID（HWID）**をペアにした**機械認証トークン**で行います。Web 画面でのログインフォーム、セッション Cookie、パスワードリセット、メール認証リンク等は**いずれも存在しません**。

そのため、Stripe Q6-1（会員ログインの不正対策）は **「該当なし」** が正しい回答となります。

---

## 2. アーキテクチャ概観

```
   [pdfHandler.exe / ZipSearch.exe / PictComp.exe]      ← クライアント（WPF デスクトップ）
              │
              │  HTTPS POST  { licenseKey, hardwareId }
              │  （認証トークン: なし。鍵自体が認証情報）
              ▼
   ┌───────────────────────────────────────────────┐
   │  Supabase Edge Functions（Deno / TypeScript）  │
   │  ・verify-license       … 検証＋アクティベーション │
   │  ・get-activations      … 端末一覧             │
   │  ・deactivate-device    … 端末解除             │
   │  ・update-device-display-name … 端末名変更      │
   └───────────────────────────────────────────────┘
              │
              │  PostgreSQL（service_role 経由・パラメータ化クエリ）
              ▼
   ┌──────────────────────────────────────────────────────────┐
   │  Supabase Postgres                                         │
   │  ・licenses     （ライセンス本体・購入情報・状態）         │
   │  ・activations  （ライセンス × 端末の利用記録・3 台上限）  │
   └──────────────────────────────────────────────────────────┘
```

**境界の特徴**:

- クライアントから Edge Function を呼ぶ際に**ユーザー認証情報を送らない**（送るのは鍵 + HWID のみ）
- service_role キーは **Edge Function の環境変数のみ**に存在し、クライアントバイナリにも GitHub 上のソースにも一切含まれない
- データベースへの直接接続も**禁止**（必ず Edge Function 経由で型と認可をチェック）

詳細な境界図は `legals/security-compliance/Security checklist for Credit/PHASE_1_3_SYSTEM_BOUNDARY.md` §3 を参照。

---

## 3. ライセンスキーの仕様

### 3-A. 32 文字コンパクト形式

| 部分 | 文字数 | 内容 |
|---|---|---|
| アプリ ID | 4 | `PDFH` ／ `ZIPS` ／ `PICT`（製品識別） |
| フォームコード | 4 | `P101` 等（販売形態識別。`P` + 2 桁数字 + 数字 1 桁） |
| 本体（HMAC 派生） | 24 | `LICENSE_SECRET_KEY` を鍵とする HMAC-SHA256 出力を Crockford Base32 エンコードした先頭 24 文字 |

実装: `supabase/functions/_shared/compact-license-key.ts`

### 3-B. 表示形式

- **DB 保存・メール表示・UI 入力**: 4 文字ごとにハイフン区切り（例: `PDFH-P101-AB12-CD34-EF56-GH78-JK90-MN12`）= 39 文字
- **検証時の正準化**: クライアントがハイフン有無のどちらで入力しても `stripSeparators()` でハイフン除去 → `toCanonicalPlain32()` で再検証 → DB 照合

### 3-C. 鍵生成は **常にサーバー側**

- 手動発行（`admin-generate-license`）と Stripe Webhook（`stripe-webhook`）の両方で **`LICENSE_SECRET_KEY`（Edge Function 環境変数）** を使った HMAC で生成
- クライアント側には鍵生成ロジックが**一切存在しない**ため、攻撃者がアプリを逆コンパイルしても**任意の有効鍵を生成不可能**

---

## 4. ハードウェア ID（HWID）

### 4-A. 採取方法

クライアント（pdfHandler.exe / 他 2 製品）が起動時にローカルで採取。

採取要素（複数を SHA-256 で連結ハッシュ化）:
- マシンの GUID（`HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid`）
- マシン名（`Environment.MachineName`）
- OS インストール時刻

→ **PII（個人を特定可能な情報）を含まない**。氏名・メール・IP アドレス・MAC アドレスは含まれない。

### 4-B. 用途

- ライセンス × HWID で **`activations` テーブルに 1 レコード**を作る → これが「アクティベーション」
- 同一ライセンスで HWID が異なる端末から接続された場合、`activations` テーブルの行が増える → **3 台超で発行を拒否**（サーバー側強制）
- HWID は**サーバー側でも平文で保存しない**（SHA-256 ハッシュのまま）

---

## 5. ライフサイクル

### 5-A. 発行（Issuance）

| 経路 | 起動者 | 関連 Edge Function |
|---|---|---|
| 手動発行（法人・銀行振込） | 運用者（本人）が `license_manager` GUI から | `admin-generate-license`（`x-admin-api-key` ヘッダ認証） |
| Stripe 自動発行（個人カード決済） | Stripe Checkout 完了 → Webhook | `stripe-webhook`（Stripe 署名検証） |

### 5-B. アクティベーション（Activation）

1. ユーザーがアプリを起動し、メールで届いたライセンスキーを「ライセンス認証」ダイアログに入力
2. アプリが HWID をローカルで採取
3. アプリが `verify-license` Edge Function に `{ licenseKey, hardwareId, deviceName, appVersion }` を POST
4. サーバーが
   - 鍵を `licenses` テーブルから検索（パラメータ化クエリ）
   - `is_active=true` を確認
   - `revoked_at` が NULL であることを確認
   - `activations` テーブルから既存アクティベーション数を取得
   - 既存に同じ HWID があれば**再認証**（成功）
   - なければ新規アクティベーション（**3 台上限まで**）
5. 成功時にクライアントへ「有効」レスポンス + 残端末数を返す
6. クライアントはライセンスキーを **Windows DPAPI で暗号化してローカルに保存**（次回起動時も `verify-license` を呼ぶ）

### 5-C. 起動時の再検証

- アプリ起動ごとに `verify-license` を呼び、サーバー側の状態（`is_active` ／ `revoked_at` ／ `activations` 整合性）と突き合わせる
- ネットワーク不通時はローカルキャッシュで一定期間（製品仕様）動作可能、復旧時に再検証

### 5-D. 端末解除（Self-service Deactivation）

- ユーザーが「ライセンス管理」UI から自端末を解除可能
- アプリが `deactivate-device` Edge Function を呼び、`activations` 行を削除
- 解除後はその端末ではアプリが認証エラーで起動不可

### 5-E. 取消（Revocation／管理者操作）

- 運用者が `license_manager` GUI から `admin-deactivate-license` Edge Function を呼ぶ
- DB 上は **物理削除しない（soft revoke）**: `is_active=false` + `revoked_at=NOW()` + `revoked_reason='...'`
- 監査追跡のため、`audit.YYYY-MM.log.jsonl`（`license_manager` の操作証跡）にも 1 行記録

---

## 6. 認可と認証の境界

| 呼び出し | 認証手段 | 認可境界 |
|---|---|---|
| クライアント → `verify-license` | なし（鍵 + HWID 自体が credential） | 鍵が DB に存在し is_active=true なら受理 |
| クライアント → `get-activations` | クライアント保有の鍵 | 自鍵に紐づく activations のみ返す |
| クライアント → `deactivate-device` | クライアント保有の鍵 | 自鍵 × 自 HWID のみ削除可 |
| 運用者 → `admin-*-license` | `x-admin-api-key` ヘッダ（`ADMIN_API_KEY` シークレット） | 一致時のみすべてのライセンス操作可 |
| Stripe → `stripe-webhook` | Stripe 署名検証（`STRIPE_WEBHOOK_SECRET`） | 検証成功時のみ DB 書込 |

→ **どのエンドポイントも、人間がユーザー名／パスワードを Web フォームから入力して認証するフローを持たない**。

---

## 7. なぜこれが Q6-1「該当なし」なのか

Stripe Q6-1（不正ログイン対策）は、**会員アカウントを持つ Web サービス**を想定した設問:
- 会員登録時の本人確認
- ログイン時のアカウントロックアウト
- パスワードリセット時の脆弱性
- ID／パスワードのリスト型攻撃対策

本サービスは:
- ❌ 会員登録の概念がない
- ❌ ログイン画面がない
- ❌ パスワードがない
- ❌ パスワードリセットがない
- ❌ Cookie・セッショントークンがない

代わりに:
- ✅ 鍵 + HWID の機械認証
- ✅ 鍵生成は HMAC でサーバー側のみ
- ✅ 端末数 3 台のサーバー側強制
- ✅ 取消フラグでの即時無効化

→ Q6-1 の各選択肢（「ログイン回数制限」「画像認証」「2 段階認証」「リスクベース認証」）は**いずれも適用文脈が存在しない**。

---

## 8. 将来の顧客ポータル実装時の影響

将来、顧客が Web からログインしてライセンス管理する**顧客ポータル**を実装する場合は、本ドキュメントを v0.2 に改訂し、以下を追加する:

- 認証方式（メールマジックリンク or OAuth or パスワード + 2FA）
- セッション管理
- アカウントロックアウト
- 不正ログイン検知
- パスワード関連の安全性（パスワード方式採用時のみ）

そのうえで、`PHASE_1_5_APPLICABLE_REQUIREMENTS_EXTRACT.md` ／ `PHASE_3_1_FINALIZED_ANSWERS.md` の **Q6-1** 回答を「該当なし」→「該当あり」へ切替え、`PHASE_4_3_…` を新規作成する。

現状（2026-04-28）の Stripe 申告には**この将来計画は含めない**（仮定の機能を提出すると Stripe 側の実装期待値とずれる）。

---

## 9. 関連実装ファイル

| ファイル | 役割 |
|---|---|
| [`supabase/functions/_shared/compact-license-key.ts`](https://github.com/6EFB0D/pdf-handler_Dev/blob/f405b73/supabase/functions/_shared/compact-license-key.ts) | 鍵生成・正規化・検証 |
| [`supabase/functions/verify-license/index.ts`](https://github.com/6EFB0D/pdf-handler_Dev/blob/f405b73/supabase/functions/verify-license/index.ts) | 鍵 + HWID の検証＋アクティベーション |
| [`supabase/functions/get-activations/index.ts`](https://github.com/6EFB0D/pdf-handler_Dev/blob/f405b73/supabase/functions/get-activations/index.ts) | 鍵に紐づく端末一覧 |
| [`supabase/functions/deactivate-device/index.ts`](https://github.com/6EFB0D/pdf-handler_Dev/blob/f405b73/supabase/functions/deactivate-device/index.ts) | 自端末解除 |
| [`supabase/functions/admin-generate-license/index.ts`](https://github.com/6EFB0D/pdf-handler_Dev/blob/f405b73/supabase/functions/admin-generate-license/index.ts) | 手動発行（`license_manager` GUI から） |
| [`supabase/functions/admin-deactivate-license/index.ts`](https://github.com/6EFB0D/pdf-handler_Dev/blob/f405b73/supabase/functions/admin-deactivate-license/index.ts) | 取消（運用者） |
| [`supabase/functions/stripe-webhook/index.ts`](https://github.com/6EFB0D/pdf-handler_Dev/blob/f405b73/supabase/functions/stripe-webhook/index.ts) | Stripe 自動発行 |

---

## 10. 改訂履歴

| 日付 | 内容 |
|---|---|
| 2026-04-28 | v0.1 初版。`PHASE_3_1_FINALIZED_ANSWERS.md` v0.4 § 4.12 の証跡として作成。Q6-1「該当なし」の構造的根拠を 1 文書にまとめ、鍵 + HWID の機械認証であってパスワード／会員ログインではないことを明示。 |

# Supabase スキーマ DEV → PROD 同期手順書

> **作成:** 2026-04-25
> **対象:** Supabase 2 プロジェクト体制（`License Manager_DEV`／`License Manager_PROD`）
> **目的:** DEV で 01〜15 のマイグレーションを当てきった状態を、空の PROD に**ベースラインとして再現**する。
> **関連:** `STRIPE_READINESS_AND_QMS_TASKS.md` Task 3／`legals/security-compliance/Security checklist for Credit/PHASE_4_1_REPO_ASSET_INVENTORY.md` 行 2（Supabase Edge Functions）

---

## 0. 前提

| 項目 | 値 |
|------|----|
| DEV プロジェクト名 | `License Manager_DEV`（旧 `yzmjuotvkxcfnsgleyxl` 相当） |
| PROD プロジェクト名 | `License Manager_PROD`（**空・新規**） |
| リージョン | AWS ap-northeast-1（両方） |
| プラン | NANO（両方） |
| 認証方式 | Supabase Dashboard（MFA 必須）|
| 接続方法 | Dashboard → SQL Editor（CLI は任意）|

---

## 1. 全体フロー（30 分目安）

```
[Step A] DEV の現況確認  ──→  99_verify_schema_state.sql を Run（DEV）
   │
   ▼
[Step B] DEV の不足を埋める ─→ 表示された欠落ファイルだけを順に Run（DEV）
   │
   ▼
[Step C] DEV ベースラインの確定 ─→ 99 を再 Run し全 true を確認（DEV）
   │
   ▼
[Step D] PROD で空状態を確認 ──→ 99 を Run（PROD・全 false 想定）
   │
   ▼
[Step E] PROD にベースラインを流す ─→ 01 → 02 → 08 → 09 → 10 → 11 → 12 → 13 → 14 → 15
   │
   ▼
[Step F] PROD の最終確認 ──→ 99 を Run し全 true を確認（PROD）
   │
   ▼
[Step G] 証跡保存 ─→ 各 Step のスクリーンショットを evidence/ に保存
```

---

## 2. Step 別チェックリスト

### Step A: DEV 現況確認

1. Supabase Dashboard → `License Manager_DEV` を開く
2. 左メニュー **SQL Editor** → **New query**
3. `docs/supabase-setup/99_verify_schema_state.sql` の内容を貼り付け → **Run**
4. 結果を 9 セクション分すべてキャプチャ（PNG）
   - 保存先: `legals/security-compliance/Security checklist for Credit/evidence/3.1/2026-04-25_§4.15_supabase_dev_baseline.png`
5. **セクション 9** の 8 列がすべて `true` なら Step C へジャンプ可

### Step B: DEV 不足分の補充（**Step A で false の列があった場合のみ**）

| セクション 9 の false 列 | 流すファイル |
|--------------------------|-------------|
| `file_01_database_schema = false` | `01_database-schema.sql`、続いて `02_rls-policies.sql` |
| `file_08_activation_count = false` | `08_add-activation-count.sql` |
| `file_09_purchased_version = false` | `09_add-purchased-version.sql` |
| `file_10_app_id = false` | `10_add_app_id.sql` |
| `file_12_subscription_removed = false` | `12_remove_subscription_model.sql`（注: `subscriptions` 行が残ると false になる）|
| `file_14_checkout_session_id = false` | `14_add_stripe_checkout_session_id.sql` |
| `file_15_manual_payment_fields = false` | `15_add_manual_payment_fields.sql` |
| `file_16_revoked_columns = false` | `16_add_revoked_columns.sql` |

> **`11_cleanup_records.sql` と `13_optional_secrets_and_schema_checks.sql`**:
> 11 はデータ整理用（任意・実環境で不要レコードがある場合のみ）、13 は確認 SQL とセクションコメントが主体（Secrets は CLI/Dashboard 設定）。**新規 PROD には適用不要**ですが、DEV で過去にコメント部だけ実行されていない場合は流しても安全（Section A だけ ALTER が走る）。

### Step C: DEV ベースライン確定

1. **99 を再 Run**
2. セクション 9 の 8 列すべて `true` を確認
3. キャプチャ → `evidence/3.1/2026-04-25_§4.15_supabase_dev_after_fill.png`

### Step D: PROD 空状態確認

1. Supabase Dashboard → `License Manager_PROD` に切り替え
2. SQL Editor で **99 を Run**
3. **想定結果**: セクション 1 で `licenses_table` / `license_activations_table` ともに `NULL` ／ セクション 9 全 `false`（ただし `file_12_subscription_removed` だけは true になる場合あり。subscriptions テーブルが最初から存在しないため。これは正常）
4. キャプチャ → `evidence/3.1/2026-04-25_§4.15_supabase_prod_empty.png`
5. **想定外の場合**（テーブルが既に存在する等）: 一旦中止し、何が起きたか確認してから次へ

### Step E: PROD ベースライン投入

PROD の SQL Editor で**この順番**に流す。各ファイルは Run 後にエラーゼロを確認してから次へ。

| 順序 | ファイル | 役割 | 想定実行時間 |
|------|----------|------|-------------|
| 1 | `01_database-schema.sql` | `licenses` / `license_activations` テーブル作成・トリガー | 〜1 秒 |
| 2 | `02_rls-policies.sql` | RLS 有効化・基本ポリシー | 〜1 秒 |
| 3 | `08_add-activation-count.sql` | `activation_count` カラム | 〜1 秒 |
| 4 | `09_add-purchased-version.sql` | `purchased_version` カラム | 〜1 秒 |
| 5 | `10_add_app_id.sql` | `app_id` カラム | 〜1 秒 |
| 6 | （スキップ可）`11_cleanup_records.sql` | 既存データ整理（PROD は空なので不要） | — |
| 7 | （スキップ可）`12_remove_subscription_model.sql` | `subscriptions` 削除（最初から無いので不要） | — |
| 8 | （任意）`13_optional_secrets_and_schema_checks.sql` | Section A の `device_name` 補完が走る | 〜1 秒 |
| 9 | `14_add_stripe_checkout_session_id.sql` | `stripe_checkout_session_id` + UNIQUE | 〜1 秒 |
| 10 | `15_add_manual_payment_fields.sql` | `payment_type` 等 6 カラム + インデックス | 〜1 秒 |

> **失敗した場合**:
> - Supabase の SQL Editor は**部分実行**になる場合があるため、エラー行以降は流れていない可能性あり
> - エラー文をスクショ → 該当ファイルだけ修正 or 該当ステートメントを個別に Run
> - **冪等性**: すべて `IF NOT EXISTS` 設計のため、再 Run は安全

### Step F: PROD 最終確認

1. PROD の SQL Editor で **99 を Run**
2. セクション 9 の 8 列が**すべて true**（`file_12_subscription_removed` は最初から `subscriptions` が無いので true）であることを確認
3. キャプチャ → `evidence/3.1/2026-04-25_§4.15_supabase_prod_after_baseline.png`
4. **Table Editor** で `licenses` / `license_activations` のカラム一覧を目視確認 → スクショ → `evidence/3.1/2026-04-25_§4.15_supabase_prod_table_editor.png`

### Step G: 証跡保存

保存先のベースパス:
```
D:\Users\admin_mak\legals\security-compliance\Security checklist for Credit\evidence\3.1\
```

| ファイル名 | 内容 |
|-----------|------|
| `2026-04-25_§4.15_supabase_dev_baseline.png` | DEV 99 の Step A 結果（任意・初回のみ） |
| `2026-04-25_§4.15_supabase_dev_after_fill.png` | DEV 99 の Step C 結果（不足を埋めた後） |
| `2026-04-25_§4.15_supabase_prod_empty.png` | PROD 99 の Step D 結果（空） |
| `2026-04-25_§4.15_supabase_prod_after_baseline.png` | PROD 99 の Step F 結果（投入後） |
| `2026-04-25_§4.15_supabase_prod_table_editor.png` | PROD Table Editor の目視確認（任意） |

これらは **Phase 3.1 Q3-2 / Q5-2**（DB セキュリティ・冗長設計）の根拠スクショとして
`legals/security-compliance/Security checklist for Credit/PHASE_3_1_FINALIZED_ANSWERS.md` §4 の**新規 §4.15** として併記する。

命名規則の詳細は `evidence/README.md` を参照。

---

## 3. PROD 投入後の必須作業（同日中）

### 3-A. Edge Functions の PROD 向けデプロイ

`pdf-handler/supabase/functions/` 直下に**11 関数**があり、全てを PROD にデプロイする。

| # | 関数名 | 役割 | `--no-verify-jwt` |
|---|--------|------|---------|
| 1 | `create-checkout-session` | Stripe Checkout セッション作成（公開 → JWT 不要） | あり |
| 2 | `request-checkout` | Checkout 直前の前処理（公開 → JWT 不要） | あり |
| 3 | `stripe-webhook` | Stripe → Supabase の Webhook 受信（署名検証） | あり |
| 4 | `verify-license` | アプリ起動時のライセンス検証（公開 API） | あり |
| 5 | `get-activations` | デバイス一覧取得（ライセンスキー認証） | あり |
| 6 | `deactivate-device` | デバイス無効化（ライセンスキー認証） | あり |
| 7 | `update-device-display-name` | デバイス表示名変更（ライセンスキー認証） | あり |
| 8 | `ping` | ハートビート（無料プラン 7 日停止対策） | あり |
| 9 | `admin-generate-license` | 手動ライセンス発行（GUI ツール用・ADMIN_API_KEY 必須） | あり |
| 10 | `admin-list-licenses` | ライセンス一覧（GUI ツール用・ADMIN_API_KEY 必須） | あり |
| 11 | `admin-deactivate-license` | ライセンス無効化（GUI ツール用・ADMIN_API_KEY 必須）※ロールバック実装時に `admin-revoke-license` へ拡張予定 | あり |

```powershell
cd D:\Users\admin_mak\project\pdf-handler

# PROD プロジェクトに link し直す（DEV と同じ手順）
npx supabase link --project-ref <PROD_PROJECT_REF>

# 11 関数すべてデプロイ（公開系・管理系すべて --no-verify-jwt：認証は関数内で行う）
npx supabase functions deploy create-checkout-session    --project-ref <PROD_PROJECT_REF> --no-verify-jwt
npx supabase functions deploy request-checkout           --project-ref <PROD_PROJECT_REF> --no-verify-jwt
npx supabase functions deploy stripe-webhook             --project-ref <PROD_PROJECT_REF> --no-verify-jwt
npx supabase functions deploy verify-license             --project-ref <PROD_PROJECT_REF> --no-verify-jwt
npx supabase functions deploy get-activations            --project-ref <PROD_PROJECT_REF> --no-verify-jwt
npx supabase functions deploy deactivate-device          --project-ref <PROD_PROJECT_REF> --no-verify-jwt
npx supabase functions deploy update-device-display-name --project-ref <PROD_PROJECT_REF> --no-verify-jwt
npx supabase functions deploy ping                       --project-ref <PROD_PROJECT_REF> --no-verify-jwt
npx supabase functions deploy admin-generate-license     --project-ref <PROD_PROJECT_REF> --no-verify-jwt
npx supabase functions deploy admin-list-licenses        --project-ref <PROD_PROJECT_REF> --no-verify-jwt
npx supabase functions deploy admin-deactivate-license   --project-ref <PROD_PROJECT_REF> --no-verify-jwt
```

> **注意**: `--no-verify-jwt` は「Supabase Auth の JWT を関数側で検証しない」設定。ライセンスキー / ADMIN_API_KEY による独自認証を関数内で行うため必要。Stripe webhook は署名検証で代替。

### 3-B. PROD 用 Secrets の登録

Dashboard → `License Manager_PROD` → Project Settings → Edge Functions → Secrets:

| キー | 値の出所 | DEV と別値必須 |
|------|----------|:---:|
| `STRIPE_SECRET_KEY` | Stripe Dashboard（**本番**用 `sk_live_...`／復活待ち）| ✅ |
| `STRIPE_WEBHOOK_SECRET` | Stripe Dashboard → Webhooks → 新規 PROD エンドポイントの whsec | ✅ |
| `STRIPE_PRICE_ID_PURCHASED` | Stripe Dashboard（本番モードの Price ID）| ✅ |
| `LICENSE_SECRET_KEY` | **新規生成**（32 文字以上のランダム英数字）| ✅ |
| `ADMIN_API_KEY` | **新規生成**（32 文字以上のランダム英数字／GUI ツールから admin-* 関数を呼ぶ際の認証）| ✅ |
| `RESEND_API_KEY` | Resend Dashboard（DEV と同一でも可だが分離推奨）| 推奨 |
| `LICENSE_EMAIL_FROM` | `Office Go Plan <noreply@office-goplan.com>`（要ドメイン検証）| ❌ |
| `APP_URL` | `https://office-goplan.com` | ❌ |
| `LICENSE_PURCHASED_MAJOR_VERSION` | `1` | ❌ |
| `SUPABASE_URL` | PROD プロジェクトの URL（`https://<PROD_REF>.supabase.co`）| ✅（自動）|
| `SUPABASE_SERVICE_ROLE_KEY` | PROD プロジェクトの service_role キー | ✅（自動）|

> **重要**:
> - `LICENSE_SECRET_KEY` / `ADMIN_API_KEY` を DEV と PROD で**同一値にしない**。DEV 側が漏えいした場合に PROD ライセンス偽造・PROD 操作が可能になるリスクを回避。
> - `SUPABASE_URL` / `SUPABASE_SERVICE_ROLE_KEY` は Supabase が **自動で関数に注入**するため Secrets 登録は不要（プロジェクトごとに自動的に PROD のものが使われる）。
> - Stripe 関連 3 キーは**Stripe アカウント復活後**に登録する。それまでは Stripe 連携を呼ばない関数（`verify-license` `get-activations` 等）は動作する。

#### Secrets 値の生成方法（PowerShell）

```powershell
# LICENSE_SECRET_KEY（64 文字の hex）
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes(($bytes = New-Object byte[] 32)); -join ($bytes | ForEach-Object { $_.ToString('x2') })

# ADMIN_API_KEY（同上・別生成）
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes(($bytes = New-Object byte[] 32)); -join ($bytes | ForEach-Object { $_.ToString('x2') })
```

生成した値は**この場で**Dashboard に登録し、ローカルにはメモ程度に控える（`backoffice/.env.prod` 等。Git 追跡対象外であることを必ず確認）。

### 3-C. backoffice ハートビートの URL 更新

`6EFB0D/backoffice` リポジトリ内 `supabase-heartbeat.yml` の URL を **PROD 側 `ping` Edge Function** に向ける。
DEV 側のハートビートは**継続**するか**停止**するかを判断（NANO で月コスト微小なため、開発継続するなら DEV も維持推奨）。

### 3-D. アプリ側の接続先更新（本番リリース時）

`pdf-handler/src/.../AppSettings.cs`（または相当する設定）の Supabase URL / AnonKey を**本番ビルド時のみ** PROD に向ける。**開発ビルドは DEV のまま**。

---

## 4. 既知の留意点

| 項目 | 留意 |
|------|------|
| **タイムゾーン** | 両プロジェクトとも UTC 内部・JST は表示時のみ。`payment_confirmed_at` の比較は UTC で。 |
| **無料プラン 7 日停止** | PROD でも発動するため、本番リリース前から backoffice ハートビートを稼働させる |
| **Stripe Webhook の URL** | DEV / PROD で URL（プロジェクト ID）が異なる。Stripe Dashboard 側に**両方**登録し、本番 / テストモードで使い分ける |
| **AnonKey / Service Role Key** | DEV と PROD で**完全に別値**。混在しないよう Secrets 名を `_DEV` `_PROD` で区別する場合は backoffice 側コードも揃える |
| **データ移行** | 本書の対象はスキーマのみ。**実データ（顧客の license レコード）の DEV→PROD 移植は本書では扱わない**。本番リリース前にテスト購入で検証する想定 |

---

## 5. 改訂履歴

| 日付 | 内容 |
|------|------|
| 2026-04-25 | v0.1 初版。DEV/PROD 2 プロジェクト体制への移行に伴い作成。99 検証 SQL を中心に Step A〜G の運用フローを定義。Edge Functions / Secrets / backoffice ハートビートの PROD 切替を §3 に併記。 |

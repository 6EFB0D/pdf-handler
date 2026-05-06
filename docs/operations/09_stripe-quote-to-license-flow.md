# 法人向け 見積 → ライセンス発行 → 請求 → 入金 フロー

> **対象:** 法人取引（売掛金 / 銀行振込）でライセンスを販売する場合の運用手順
> **改訂:** 2026-04-25 全面書き直し（5/1 案件のフロー再定義 + Stripe Invoicing 経路の追記）
> **適用ケース:** 5/1 法人納品 1 件目、および以降の B2B 案件
> **関連:**
> - `STRIPE_READINESS_AND_QMS_TASKS.md` Task 5/6/8/9/9.5/10
> - `legals/security-compliance/Security checklist for Credit/snapshots/2026-04-25_5-1_DELIVERY_STRATEGY_v2.md`
> - `D:\Users\admin_mak\backoffice\License generator\GUI_TOOL_DESIGN.md`
> - `legals/利用規約.md` 第 5 条の 2（B2B 売掛金条項・支払期日 30 日・遅延損害金 14.6%）
> - `pdf-handler/docs/operations/06_manual-license-issuance.md`（手動発行の業務手順）
> - `pdf-handler/docs/operations/07_admin-functions-setup.md`（GUI ツールのセットアップ）

---

## 0. 経路の選択フローチャート

```
法人案件の打診を受けた
       │
       ▼
顧客は Stripe での支払いを希望するか？
   │                       │
   │ NO（売掛金 / 銀振）   │ YES（クレジット）
   ▼                       ▼
[フェーズ A]           [Stripe Checkout 経路]
手動 GUI フロー        通常の B2C と同じ。
                       create-checkout-session で完結。
       │
       ▼
Stripe アカウントは復活済か？
   │                       │
   │ NO（停止中）          │ YES
   ▼                       ▼
A-1: GUI のみで完結    A-2: Stripe Invoicing 併用
（Phase 1 機能のみ）    （Phase 2 機能を使う）
```

**本書の主たる対象**: フェーズ A（A-1 / A-2 とも）。Stripe Checkout はここでは扱わない。

---

## 1. 全体タイムライン（標準ケース）

```
Day 0     事前メール・打診（Task 8）
   │
   ▼
Day +N    見積書 PDF + アプリ納品ビルド を顧客に送付（メール 1 通）
   │       └→ 顧客は試用 / 評価 / 社内決裁
   │
   ▼
発注当日   GUI ツールでライセンス発行 + 請求書 PDF を顧客にメール送付
   │       └→ 顧客は当日からアクティベート可能
   │
   ▼
締め日    売掛金確定（標準: 月末締め）
   │
   ▼
入金期限   銀行振込（標準: 翌月末・契約により 30 日以内）
   │       └→ 入金確認 → 領収書 PDF を顧客にメール送付
   │
   ▼
完了      取引クローズ・evidence/ 保管
```

---

## 2. フェーズ A-1: 手動 GUI フロー（Stripe 停止中）

**前提**: Stripe アカウントが未復活。Supabase + GUI ツールのみで運用。

### Step 1: 見積書の発行（オフライン）

- ひな形: `legals/templates/見積書_ひな形.docx`
- 採番ルール: `Q-YYYYMMDD-NNN`（例: `Q-20260501-001`）
- 記載必須項目:
  - 商品名: `pdfHandler Standard版 ライセンス × N 本`
  - 単価: ¥4,980（消費税不課税）
  - 10 ライセンス合計: ¥49,800（消費税不課税・収入印紙不要）
  - 合計金額・**支払期限（標準: 月末締め翌月末払い）**・**振込先口座**
  - 第 5 条の 2 に基づく旨を備考に明記
- PDF 化 → 顧客にメール送付
- 控え: `evidence/YYYY-MM-DD_quote_Q-YYYYMMDD-NNN.pdf`

### Step 2: アプリ納品ビルドの送付

- pdfHandler 製品版バイナリ（または試用ビルド）を GitHub Releases or 直接添付で送付
- 同一メールに見積書 PDF を併送するのが標準

### Step 3: 発注受領

- 顧客から **PO（発注書）or メール承諾**を受領
- PO 番号を記録（`notes` 欄に書き込む用）

### Step 4: GUI でライセンス発行（発注当日）

> **必須前提**: フェーズ 1 の GUI（`main.py` 拡張版）が完成済み。`MIGRATION_DEV_TO_PROD.md` で PROD ベースライン投入完了済み。

#### 4-1. GUI 起動と環境確認
1. `cd D:\Users\admin_mak\backoffice\License generator && python main.py`
2. 起動時の **PROD 確認ダイアログ**で「PROD で続行」を押す
3. ステータスバーが **🔴 PROD — License Manager_PROD** で**赤背景**になっていることを目視確認

#### 4-2. 発行フォーム入力
| 項目 | 値 |
|------|-----|
| App | `PDFH — pdfHandler` |
| FormCode | `P101`（買い切り標準）or `P201`（複数本パック）|
| User Email | 顧客のサポート連絡先メール |
| Customer Company | 法人名（正式名称・株式会社等含む） |
| Customer Contact | 担当者名 |
| Invoice Number | **次手順**で発行する請求書番号（`INV-YYYYMMDD-NNN`）|
| Payment Type | `manual` |
| Is Active | `true`（発注受領済のため有効化）|
| Notes | `PO 番号: 〔顧客発注番号〕／ 売掛金条項適用 ／ 締め日: YYYY-MM-DD ／ 入金期日: YYYY-MM-DD` |

#### 4-3. 実行前確認ダイアログ
- PROD では「PROD」と入力させる二重確認が走る
- 入力内容のプレビューを目視 → [実行] を押す

#### 4-4. 結果の保管
- レスポンス JSON（`licenseId` / `licenseKeyDisplay` / `emailSent`）を取得
- スクリーンショットを `evidence/YYYY-MM-DD_license_INV-YYYYMMDD-NNN.png` に保存
- 監査ログ（`audit.YYYY-MM.log.jsonl`）にも自動記録される

### Step 5: 請求書の発行・送付（発注当日 or 翌営業日）

- ひな形: `legals/templates/請求書_ひな形.docx`
- 採番: `INV-YYYYMMDD-NNN`（Step 4-2 の `Invoice Number` と一致させる）
- 記載必須項目:
  - 商品名・単価・数量・合計金額
  - **支払期限**・振込先口座
  - **第 5 条の 2 に基づく旨**（遅延損害金 14.6% を明記）
- PDF 化 → ライセンスキーと**同一メール**で送付（顧客の事務負担を最小化）
- 控え: `evidence/YYYY-MM-DD_invoice_INV-YYYYMMDD-NNN.pdf`

### Step 6: 入金確認（締め日翌月末まで）

- SMBC ネットバンク or 通帳で入金を確認
- 入金額・振込人名・入金日を記録
- **GUI で `payment_confirmed_at` を更新**（admin-update-license があれば）または直接 Dashboard SQL Editor で UPDATE:
  ```sql
  UPDATE licenses
  SET payment_confirmed_at = NOW(),
      notes = notes || E'\n入金確認: YYYY-MM-DD / 振込人: 〔名義〕 / 金額: ¥XX,XXX'
  WHERE license_key = '〔該当キー〕';
  ```
- スクショ: `evidence/YYYY-MM-DD_payment_INV-YYYYMMDD-NNN.png`

### Step 7: 領収書発行・送付

- ひな形: `legals/templates/領収書_ひな形.docx`
- 採番: `R-YYYYMMDD-NNN`
- 収入印紙: 電子発行なら通常不要。紙の領収書でも 10 ライセンス（¥49,800）は 5 万円未満のため不要（2026-04 時点）
- PDF 化 → メール送付
- 控え: `evidence/YYYY-MM-DD_receipt_R-YYYYMMDD-NNN.pdf`

### Step 8: 取引クローズ

- evidence/ 配下に**見積・請求・入金・領収・ライセンス発行記録**の 5 点が揃っていることを確認
- `notes` 欄に `STATUS: CLOSED YYYY-MM-DD` を追記

---

## 3. フェーズ A-2: Stripe Invoicing 経路（Stripe 復活後）

**前提**: Stripe アカウント復活済 / GUI ツールのフェーズ 2 機能（`STRIPE_READINESS_AND_QMS_TASKS.md` Task 5-5）実装済。

### 経路の意義

> Stripe 復活後も、**B2B は売掛金 / 銀行振込**のスタンスを維持。
> ただし**見積書発行・請求書発行・入金管理**を Stripe 上で完結させ、Webhook でライセンスを自動発行する。
> 紙の Word ひな形運用を**徐々に縮退**させる。

### 全体構成

```
[GUI ツール]                  [Stripe]                      [Supabase]
   │                            │                              │
   │ 1. Stripe Quote 作成       │                              │
   │ ─────────────────→         │                              │
   │                            │ Quote PDF を顧客にメール     │
   │                            │                              │
   │ 2. 顧客発注後 Quote accept │                              │
   │ ─────────────────→         │                              │
   │                            │ → Invoice 自動生成           │
   │                            │ → Invoice PDF 送付            │
   │                            │                              │
   │ 3. （顧客が銀行振込）       │                              │
   │                            │                              │
   │ 4. Invoice を mark as paid │                              │
   │   (paid_out_of_band)        │                              │
   │ ─────────────────→         │                              │
   │                            │ ─ event: invoice.paid ──→    │
   │                            │                              │ stripe-webhook
   │                            │                              │ → admin-generate-license
   │                            │                              │ → 顧客にメール
   │                            │                              │
   │ 5. 領収書 PDF を Stripe から取得 → 顧客にメール             │
   │ ─────────────────→         │                              │
```

### Step 別差分（A-1 との対比）

| Step | A-1（手動）| A-2（Stripe Invoicing）|
|------|-----------|----------------------|
| 見積発行 | Word ひな形 → PDF → メール手動送付 | GUI から Stripe Quote を作成 → 顧客に自動メール |
| 発注受領 | メール / PO 受領 | GUI で Quote accept → Invoice 自動生成 |
| ライセンス発行 | GUI で `admin-generate-license` 直接呼び出し | `invoice.paid` Webhook で自動発行 |
| 請求書発行 | Word ひな形 → PDF | Stripe Invoice PDF（自動）|
| 入金管理 | 通帳 / ネットバンクで目視 | GUI から `mark as paid (out of band)` |
| 領収書 | Word ひな形 → PDF | Stripe Invoice PDF を流用 |

### Webhook 拡張（既存資産の改修）

`pdf-handler/supabase/functions/stripe-webhook/index.ts` に**`invoice.paid` ハンドラ**を追加:

```typescript
// 既存: case 'checkout.session.completed':
case 'invoice.paid': {
  const invoice = event.data.object as Stripe.Invoice;
  // metadata から appId / formCode / customerCompany を取り出す
  const appId           = invoice.metadata?.appId           ?? 'PDFH';
  const formCode        = invoice.metadata?.formCode        ?? 'P101';
  const customerCompany = invoice.metadata?.customerCompany ?? null;
  const customerContact = invoice.metadata?.customerContact ?? null;

  // 既存の admin-generate-license と同じロジックでライセンス発行
  // payment_type は 'stripe' に（Stripe 経由のため）
  // invoice_number は invoice.number（Stripe 採番）を流用
  // ...
  break;
}
```

> **重要**: `invoice.metadata` は GUI からの Quote 作成時に**必ず付与**する。付与漏れは Webhook で 422 を返してフェイルセーフ。

---

## 4. メール送付テンプレ

### 4-A. 見積書 + アプリ納品（Step 1〜2 同時）

```
件名: 【Office Go Plan】pdfHandler 見積書および納品ビルドのご送付

〇〇株式会社
〇〇部 〇〇様

平素よりお世話になっております。Office Go Plan です。

ご相談いただきました pdfHandler について、以下のとおり
見積書と納品ビルドをお送りします。

【添付】
  ・見積書（Q-YYYYMMDD-NNN）.pdf
  ・pdfHandler セットアップ.zip（または GitHub Releases URL）

【支払条件】
  当方の利用規約 第 5 条の 2 に基づき、以下の条件でご請求します。
  ・締め日: 月末
  ・支払期日: 翌月末（銀行振込）
  ・遅延損害金: 年 14.6%

ライセンスキーは発注いただいた当日に発行・送付いたします。
発注はメールへのご返信、または PO の送付でも承ります。

ご検討のほどよろしくお願いいたします。

───────────────────────
Office Go Plan
support@office-goplan.com
```

### 4-B. ライセンス発行 + 請求書（Step 4〜5 同時）

```
件名: 【Office Go Plan】pdfHandler ライセンスキーおよび請求書のご送付

〇〇株式会社
〇〇部 〇〇様

ご発注ありがとうございます。
ライセンスキーと請求書をお送りいたします。

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
■ ライセンスキー
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
PDFH-P101-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XXXXXXXX...

対応台数: 最大 3 台
有効期限: 無期限（買い切り）

【アクティベーション手順】
  1. pdfHandler を起動
  2. ヘルプ → ライセンス認証 を開く
  3. 上記キーを貼り付けて「認証」をクリック

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
■ 請求書
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
請求書番号: INV-YYYYMMDD-NNN（添付 PDF）
お支払期日: YYYY-MM-DD
振込先   : 〇〇銀行 〇〇支店 普通 XXXXXXX 名義: 〔口座名義〕

ご入金確認後、領収書をお送りいたします。

───────────────────────
Office Go Plan
support@office-goplan.com
```

### 4-C. 入金確認 + 領収書（Step 6〜7 同時）

```
件名: 【Office Go Plan】ご入金確認および領収書のご送付

〇〇株式会社
〇〇部 〇〇様

このたびは、ご入金いただきありがとうございました。
領収書を添付にてお送りいたします。

【添付】
  ・領収書（R-YYYYMMDD-NNN）.pdf

今後とも pdfHandler をどうぞよろしくお願いいたします。

───────────────────────
Office Go Plan
support@office-goplan.com
```

---

## 5. エビデンス保管（Phase 5.1 連動）

| # | 書類 | ファイル名 | Phase 5.1 トレース |
|---|------|-----------|--------------------|
| 1 | 見積書控え | `evidence/YYYY-MM-DD_quote_Q-YYYYMMDD-NNN.pdf` | G7 §5.1-1 |
| 2 | アプリ納品メール（送信済記録）| `evidence/YYYY-MM-DD_delivery_email_*.eml` | G7 §5.1-2 |
| 3 | 発注書 / 受注確認メール | `evidence/YYYY-MM-DD_PO_*.pdf` or `.eml` | G7 §5.1-3 |
| 4 | ライセンス発行記録（GUI スクショ + 監査ログ抜粋）| `evidence/YYYY-MM-DD_license_INV-YYYYMMDD-NNN.png` | G3 §5.1-4 |
| 5 | 請求書控え | `evidence/YYYY-MM-DD_invoice_INV-YYYYMMDD-NNN.pdf` | G7 §5.1-5 |
| 6 | 入金通知 / 通帳画面 | `evidence/YYYY-MM-DD_payment_*.png` | G7 §5.1-6 |
| 7 | 領収書控え | `evidence/YYYY-MM-DD_receipt_R-YYYYMMDD-NNN.pdf` | G7 §5.1-7 |

---

## 6. リスクと対応

| リスク | 確率 | 影響 | 対応 |
|--------|------|------|------|
| 顧客が支払期限を過ぎても入金しない | 低 | 売掛残高の長期化 | 第 5 条の 2 §5（遅延損害金 14.6%）を**書面で督促**。30 日経過後に**ライセンス停止**（GUI の取消機能で `is_active=false`）|
| 発注後にライセンス発行ミス（誤宛先メール等）| 中 | 顧客の混乱 | GUI の**取消機能（soft revoke）**で即時無効化 → 正しい宛先に再発行 |
| Stripe Invoicing で `invoice.metadata` 付与漏れ | 中 | Webhook で発行スキップ | Webhook 側で metadata 必須チェック → 422 を返して GUI で再送 |
| GUI で誤って PROD にテストデータ投入 | 中 | 本番 DB 汚染 | 環境表示 3 重防御 + PROD は二重確認（`STRIPE_READINESS_AND_QMS_TASKS.md` Task 5-2/5-3）|
| 領収書の収入印紙貼り忘れ | 低 | 法令違反 | 10 ライセンス（¥49,800）は印紙不要。紙発行かつ 5 万円以上の案件だけ `notes` に「印紙貼付要」を記入し、出力前にチェック |

---

## 7. 改訂履歴

| 日付 | 内容 |
|------|------|
| 2026-04-25 v1 | 初版作成（5/1 法人納品の暫定手順）。Supabase 直接 INSERT を前提。 |
| 2026-04-25 v2 | **全面書き直し**。フロー再定義（5/1 = 見積+アプリ、発注後にライセンス発行、6/末入金）に伴い 8 ステップで再構成。GUI ツール経由を標準化、Stripe Invoicing 経路（フェーズ A-2）を併記。第 5 条の 2 を支払条件に明記。エビデンス保管を 7 点に拡張し Phase 5.1 トレース行列と連動。 |

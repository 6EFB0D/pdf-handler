# 5ライセンスパック（サブスクリプション）検証手順書

> **PDF Handler 本体では未採用**（未公開・Stripe はテストのみ・販売は買い切りのみ）。将来の別商品やパック販売の設計メモとして残しています。

## 概要

本ドキュメントは、ECサイト等で5ライセンスパックのサブスクリプションを販売するシナリオの検証手順をまとめたものです。

### 対象シナリオ

- **商品**: 5ライセンスパック（Standard版サブスクリプション）
- **契約開始日**: 最初の1ライセンスのアクティベート日から起算
- **有効期間**: 最初のアクティベート日から1年間
- **請求**: パック単位で管理（アクティベート日がバラバラでも請求・更新日は統一）

---

## 前提条件

### 実装前の確認事項

5ライセンスパックをEC販売するには、以下の拡張が必要です。

| 項目 | 現状 | 必要対応 |
|------|------|----------|
| Stripe商品 | 1ライセンス単体のみ | 5ライセンスパック用のPrice/Productを追加 |
| create-checkout-session | quantity=1固定 | quantity=5 または パック用Price IDに対応 |
| stripe-webhook | 1ライセンス1キー発行 | 5キー一括発行、またはパックIDと紐付け |
| licensesテーブル | 1行=1ライセンス | パック単位の親レコード、または5行まとめて作成 |
| 契約開始日 | checkout.session.completed時点 | 最初のアクティベート日から起算に変更 |

### 検証環境

- **Supabase**: プロジェクト `yzmjuotvkxcfnsgleyxl`
- **Stripe**: テストモード
- **アプリ**: デバッグビルド

---

## 検証フェーズ1: Stripe商品・Priceの作成

### 1.1 5ライセンスパック用Productの作成

1. Stripeダッシュボード → Products → Add product
2. 以下の内容で作成:
   - **Name**: PDFハンドラ Standard 5ライセンスパック（サブスクリプション）
   - **Description**: 5台分のライセンス。最初のアクティベート日から1年間有効。
3. Pricing:
   - **Recurring**: 年払い
   - **Price**: 例 8,000円/年（1ライセンス1,600円×5の例）
4. **Save product** → Price ID を控える（例: `price_xxx`）

### 1.2 環境変数の設定

Supabase Edge FunctionsのSecretsに以下を追加:

```
STRIPE_PRICE_ID_5PACK_SUBSCRIPTION=price_xxx
```

### 1.3 create-checkout-sessionの拡張

`create-checkout-session` に以下を追加（実装時）:

- `plan: "Standard5PackSubscription"` を受け付け
- `priceIds` に `Standard5PackSubscription` をマッピング
- `metadata.plan` に `Standard5PackSubscription` を設定

---

## 検証フェーズ2: 購入〜キー発行フロー

### 2.1 購入操作

1. アプリ（またはECサイト）から「5ライセンスパック」の購入を開始
2. Stripe Checkoutで支払い完了
3. `checkout.session.completed` Webhookが発火

### 2.2 Webhook処理の検証ポイント

**現行仕様（1ライセンス）**:
- 1つの `license_key` を発行
- `activation_date` = checkout完了日時
- `subscription_renewal_date` = checkout完了日から1年後

**5ライセンスパック仕様（案）**:
- 5つの `license_key` を発行（または1キーで5デバイス分の権限を持つ仕様に変更）
- `activation_date` = NULL（未アクティベート）
- `subscription_renewal_date` = NULL → **最初のアクティベート時に設定**
- パックID（`license_pack_id` 等）で5ライセンスを紐付け

### 2.3 購入時メール送信

- 5つのライセンスキーをメールで送付
- 「最初の1つをアクティベートした日から1年間有効」である旨を明記

---

## 検証フェーズ3: 最初のアクティベート日から起算

### 3.1 verify-licenseの拡張

最初のアクティベートが発生した時点で:

1. 同一パック内の全ライセンスの `subscription_renewal_date` を設定
2. 起算日 = このアクティベート日時
3. 更新日 = 起算日 + 1年

### 3.2 検証手順

1. **T0**: 5ライセンスパックを購入
   - 5つのキーが発行される
   - `subscription_renewal_date` は NULL または未設定

2. **T1** (7日後): ユーザーAがキー1をアクティベート
   - `subscription_renewal_date` = T1 + 1年 に設定（パック内全キー）
   - キー1: 有効、更新日 = T1+1年

3. **T2** (14日後): ユーザーBがキー2をアクティベート
   - キー2も有効、更新日 = キー1と同じ（T1+1年）
   - キー2のアクティベート日はT2だが、契約期間はT1基準

4. **T1+1年**: 更新日到来
   - パック全体のサブスクが更新対象
   - Stripeから更新請求
   - 更新後、新しい `subscription_renewal_date` をパック内全キーに反映

### 3.3 請求との整合性

- Stripeサブスクリプションの `current_period_start` / `current_period_end` を最初のアクティベート日基準で設定する必要あり
- 購入時点では `start_date` を保留し、最初のアクティベート時に Stripe API で `subscription.start_date` を更新するか、別途「アクティベート日トリガー」のバッチ処理でStripe側と同期する設計が考えられる

---

## 検証フェーズ4: アプリ側の確認

### 4.1 キー1のアクティベート

1. アプリ起動 → ヘルプ → ライセンス → ライセンスキーを入力
2. キー1を入力 → アクティベート
3. 「Standard版（サブスクリプション）」と表示され、更新日が1年後であることを確認

### 4.2 キー2のアクティベート（別PCまたは別ユーザー）

1. 別環境でキー2をアクティベート
2. 同様に有効となり、更新日がキー1と同じであることを確認

### 4.3 ライセンス管理での確認

1. ヘルプ → ライセンス → デバイス管理
2. 同一キーで最大3デバイスまで登録可能であることを確認
3. （5パックの場合）5つのキーはそれぞれ独立して3デバイスまで、計15デバイスまで利用可能

---

## 検証チェックリスト

- [ ] Stripeに5ライセンスパック用Product/Priceが作成されている
- [ ] create-checkout-sessionでパックプランを選択できる
- [ ] checkout.session.completedで5キーが発行される
- [ ] 購入時点では subscription_renewal_date が未設定
- [ ] 最初のアクティベートで subscription_renewal_date が設定される
- [ ] 2つ目以降のアクティベートでも同じ更新日が維持される
- [ ] アプリで各キーが有効にアクティベートできる
- [ ] 特商法で要求される表示（役務提供時期等）が購入画面にある

---

## 参考: データベーススキーマ案

5ライセンスパック対応の場合、以下のいずれかまたは併用を検討:

### 案A: license_packsテーブルを追加

```sql
CREATE TABLE license_packs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  stripe_subscription_id TEXT,
  first_activation_date TIMESTAMPTZ,
  subscription_renewal_date TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- licensesに pack_id を追加
ALTER TABLE licenses ADD COLUMN pack_id UUID REFERENCES license_packs(id);
```

### 案B: 既存licensesのみで管理

- 同一 `stripe_subscription_id` を持つ5行のlicensesを作成
- 最初に `activation_date` が入った行の日付を基準に、同subscription_idの全行の `subscription_renewal_date` を一括更新

---

## 次のステップ

1. 上記スキーマ・Webhookの拡張を実装
2. 法的問題チェック資料（`06-legal-considerations-license-pack.md`）を確認
3. 契約書・規約への記載（`07-paid-license-agreement-supplement.md`）を反映
4. 本手順書に沿って検証を実施

# 5ライセンス買い切り（請求書払い/ECパッケージ）検証手順書

## 概要

本ドキュメントは、**ライセンスメニューから請求書発行要請が届いた**ことを前提に、5ライセンス買い切りを請求書払いまたはECサイトでパッケージ販売する際の検証手順をまとめたものです。

本手順は**管理者（販売側）**向けの検証・発行フローです。

---

## 前提条件

### 顧客からの問い合わせ経路

1. 顧客がアプリを起動
2. 「ヘルプ」→「ライセンス」をクリック
3. 「まとめ買い・請求書払い」セクションの「サポートへ問い合わせる」をクリック
4. ブラウザで ContactUrl（問い合わせ先）が開く
5. 顧客がフォームやメールで以下の情報を送信:
   - 会社名
   - 担当者名
   - メールアドレス
   - 必要なライセンス数（本手順では5）
   - 希望支払方法（請求書払い または ECサイトでの購入）

### 管理者が確認すべき内容

- 問い合わせメールまたはフォーム送信が届いている
- ライセンス数: 5
- プラン: Standard版（買い切り）
- 連絡先メールアドレス

---

## 検証フェーズA: 請求書払いの場合

### ステップ1: ライセンス作成（Supabase）

1. [Supabase ダッシュボード](https://supabase.com/dashboard/project/yzmjuotvkxcfnsgleyxl) にログイン
2. 左メニューから「SQL Editor」を開く
3. 以下のSQLを実行（`customer@example.com` を問い合わせのメールアドレスに置き換える）:

```sql
-- 5ライセンス買い切りを手動発行（請求書払い用）
INSERT INTO licenses (license_key, plan, user_email, is_active)
SELECT generate_license_key(), 'purchased', 'customer@example.com', true
FROM generate_series(1, 5);
```

4. 発行された5つのライセンスキーを取得:

```sql
SELECT license_key, created_at 
FROM licenses 
WHERE user_email = 'customer@example.com' 
ORDER BY created_at DESC 
LIMIT 5;
```

5. 発行された5つの `license_key` を控える（顧客への送付用）

### ステップ2: 請求書発行（業務フロー）

1. **金額の計算**
   - 単価: 3,480円（Standard版買い切り）
   - 5ライセンス合計: 17,400円（税別の場合。税込みの場合は価格ポリシーに従う）

2. **請求書の発行**
   - 自社の請求書フォーマットで作成
   - 顧客情報（会社名、担当者、連絡先）を記載
   - 明細: PDFハンドラ Standard版（買い切り） × 5 = 17,400円
   - 支払期限を明記

3. **請求書の送付**
   - メール添付または郵送で顧客に送付

### ステップ3: 入金確認後、ライセンスキー送付

1. 入金の確認（振込確認、着金通知など）
2. ステップ1で発行した5つのライセンスキーをメールで送付
3. 利用手順を案内:
   - 各PCでアプリを起動
   - 「ヘルプ」→「ライセンス」→「ライセンスキーを入力」
   - ライセンスキーを入力して「アクティベート」をクリック
   - 1つのライセンスキーで最大3台のPCまでアクティベート可能

### ステップ4: 検証

1. **顧客によるアクティベート確認**
   - 顧客が5つのキーのいずれかをアクティベート
   - アプリに「Standard版（買い切り）」「ライセンス有効」と表示されることを確認

2. **デバイス数制限の確認**
   - 同一キーで3台までアクティベート可能であることを案内
   - 「ヘルプ」→「ライセンス」→「デバイス管理」で登録デバイスを確認できることを案内

3. **買い切りとして永続利用可能**
   - 有効期限なしで利用できることを確認

---

## 検証フェーズB: ECサイト パッケージ販売の場合

### 方式1: 手動発行（請求書払いに近い運用）

ECサイトで「5ライセンスパック」商品を販売し、購入完了後に管理者が手動で5キーを発行する運用:

1. ECサイト（Stripe Checkout、BASE、STORES 等）で5ライセンスパック商品を作成
2. 顧客が購入・決済完了
3. 管理者に購入通知が届く
4. 検証フェーズAのステップ1と同様に、Supabaseで5ライセンスを発行
5. 発行した5キーを顧客のメールアドレスに送付

### 方式2: Stripe自動化（要実装拡張）

Stripe決済と連携して自動で5キーを発行する場合:

| 現状 | 必要対応 |
|------|----------|
| `create-checkout-session` は quantity=1 固定 | quantity=5 または 5ライセンス用Price IDに対応 |
| `stripe-webhook` は 1キーのみ発行 | quantityに応じて5キーを一括発行するロジックを追加 |
| licensesテーブル 1行=1キー | 5行をまとめてINSERT |

複数キー一括発行の設計メモは [05-license-pack-subscription-verification.md](./05-license-pack-subscription-verification.md) にあります（**本アプリでは未採用の参考文書**。買い切りパックは同様に「複数行 INSERT」で拡張する想定です）。

---

## 付録: SQLリファレンス

### 5ライセンス一括発行

```sql
-- customer@example.com を実際のメールアドレスに置き換える
INSERT INTO licenses (license_key, plan, user_email, is_active)
SELECT generate_license_key(), 'purchased', 'customer@example.com', true
FROM generate_series(1, 5);
```

### 発行したキーの取得

```sql
SELECT license_key, created_at 
FROM licenses 
WHERE user_email = 'customer@example.com' 
ORDER BY created_at DESC 
LIMIT 5;
```

### テスト用ライセンスの削除

```sql
-- 注意: 関連するlicense_activationsもCASCADEで削除されます
DELETE FROM licenses 
WHERE user_email = 'customer@example.com' 
AND stripe_customer_id IS NULL 
AND stripe_payment_intent_id IS NULL;
```

---

## 検証チェックリスト

- [ ] 問い合わせ内容（会社名・メール・ライセンス数）を確認
- [ ] Supabaseで5ライセンスを作成
- [ ] 5つのライセンスキーを取得・控える
- [ ] 請求書を発行・送付（請求書払いの場合）
- [ ] 入金確認（請求書払いの場合）
- [ ] 5つのライセンスキーを顧客に送付
- [ ] 顧客が各キーをアクティベートできることを検証
- [ ] デバイス管理で1キーあたり最大3台までであることを確認
- [ ] 買い切りとして永続利用できることを確認

---

## 関連ドキュメント

- [docs/supabase-setup/01_database-schema.sql](../supabase-setup/01_database-schema.sql) - licensesテーブル、generate_license_key()
- [docs/testing/license-testing-guide.md](../testing/license-testing-guide.md) - ライセンス検証の既存手順
- [docs/enterprise/license-key-purchase-guide.md](../enterprise/license-key-purchase-guide.md) - エンタープライズ向け購入フロー

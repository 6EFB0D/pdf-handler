# 企業端末での決済・ライセンス認証テスト手順書

企業の端末にインストールした状態で、決済とライセンス認証が動作するかを確認するための手順です。機能・取説・価格設定・メール案内・サポート体制などに残課題があっても、**決済と認証の動作確認**を先行して行えます。

---

## 1. 事前準備（テスト実施前に完了すること）

### 1.1 Supabase Edge Functions のデプロイ

以下の Edge Functions が Supabase にデプロイ済みであること：

| 関数名 | 用途 |
|--------|------|
| `create-checkout-session` | 決済ページ（Stripe Checkout）のURLを発行 |
| `verify-license` | ライセンスキーの検証・アクティベーション |
| `stripe-webhook` | Stripe 決済完了時のライセンス作成 |

**デプロイ手順**: [docs/supabase-setup/deployment-checklist.md](../supabase-setup/deployment-checklist.md) を参照

**確認方法**: [Supabase ダッシュボード](https://supabase.com/dashboard/project/yzmjuotvkxcfnsgleyxl) → Edge Functions で3つとも「Active」であること

### 1.2 Supabase 環境変数

Edge Functions 用の Secrets が設定されていること：

- `STRIPE_SECRET_KEY`
- `STRIPE_WEBHOOK_SECRET`
- `STRIPE_PRICE_ID_PURCHASED`（買い切り・必須）
- `APP_URL`（Checkout 成功/キャンセル後のリダイレクト先）

### 1.3 Stripe Webhook

[Stripe ダッシュボード](https://dashboard.stripe.com/webhooks) で：

- エンドポイント: `https://yzmjuotvkxcfnsgleyxl.supabase.co/functions/v1/stripe-webhook`
- イベント: `checkout.session.completed`
- Signing secret が Supabase の `STRIPE_WEBHOOK_SECRET` に設定されていること

---

## 2. アプリのビルド・配布

### 2.1 単体 exe のビルド

```powershell
cd D:\Users\admin_mak\project\pdf-handler
dotnet publish src\PdfHandler.UI\PdfHandler.UI.csproj -c Release -r win-x64 --self-contained true -o publish\win-x64
```

出力先: `publish\win-x64\PdfHandler.UI.exe`

### 2.2 環境変数について

アプリは **環境変数なし** でも動作します。

- `SUPABASE_ANON_KEY`: 未設定の場合は `App.xaml.cs` のフォールバック値を使用
- `SUPABASE_ANON_KEY` を設定したい場合（本番用など）:
  - システム環境変数に設定
  - または exe 起動用のバッチファイルで設定:
    ```batch
    set SUPABASE_ANON_KEY=eyJxxxxx
    PdfHandler.UI.exe
    ```

### 2.3 企業端末への配布

1. `publish\win-x64` フォルダ一式を ZIP で配布
2. または USB メモリでコピー
3. 企業端末で解凍し、`PdfHandler.UI.exe` を直接実行（インストーラー不要）

---

## 3. ネットワーク要件（企業環境）

アプリが以下の URL にアクセスできる必要があります：

| 用途 | URL | 備考 |
|------|-----|------|
| 決済 | `https://yzmjuotvkxcfnsgleyxl.supabase.co/functions/v1/create-checkout-session` | Checkout URL 取得 |
| 決済 | `https://checkout.stripe.com/*` | ブラウザで開く |
| ライセンス認証 | `https://yzmjuotvkxcfnsgleyxl.supabase.co/functions/v1/verify-license` | ライセンスキー検証 |

**ファイアウォール・プロキシ** でブロックされていないか、事前に IT 部門に確認してください。

---

## 4. テスト手順

### 4.1 決済テスト（フルフロー）

1. 企業端末で `PdfHandler.UI.exe` を起動
2. 「ヘルプ」→「購入」をクリック
3. 「Standard版（買い切り）」の「購入する」ボタンをクリック
4. **期待**: ブラウザで Stripe Checkout ページが開く
5. Stripe テストカードで決済:
   - カード番号: `4242 4242 4242 4242`
   - 有効期限: 任意の未来（例: `12/34`）
   - CVC: 任意の3桁（例: `123`）
6. 「購入を完了」をクリック
7. **期待**: 成功ページが表示される
8. Supabase ダッシュボードで `licenses` テーブルに `PDFH-` で始まるライセンスキーが生成されていることを確認

**失敗時の確認**: [docs/troubleshooting/checkout-debugging.md](../troubleshooting/checkout-debugging.md)

### 4.2 ライセンス認証テスト（決済をスキップ）

決済を経由せず、**ライセンス認証のみ**を試したい場合：

#### 方法A: Supabase で手動ライセンスを作成

1. [Supabase ダッシュボード](https://supabase.com/dashboard/project/yzmjuotvkxcfnsgleyxl) を開く
2. Table Editor → `licenses` テーブル
3. 「Insert row」をクリック
4. 以下の値を入力:

   | カラム | 値 |
   |--------|-----|
   | `license_key` | `PDFH-P101-` + 28文字（例: `PDFH-P101-A1B2C3D4E5F6G7H8I9J0K1L2M3`） |
   | `plan` | `purchased` |
   | `user_email` | `test@example.com` |
   | `is_active` | `true` |

5. その他のカラムは空またはデフォルトで可
6. 「Save」をクリック

#### 方法B: SQL でライセンスを作成

Supabase SQL Editor で実行:

```sql
INSERT INTO licenses (license_key, plan, user_email, is_active, purchased_version)
VALUES (
  'PDFH-P101-' || UPPER(SUBSTRING(REPLACE(gen_random_uuid()::TEXT, '-', ''), 1, 28)),
  'purchased',
  'test@example.com',
  true,
  '1'
)
RETURNING license_key;
```

表示された `license_key` をコピーしてアプリで入力します。

#### アプリ側の操作

1. アプリを起動
2. 「ヘルプ」→「ライセンス」をクリック
3. 「ライセンスキーを入力」をクリック
4. 上記で作成したライセンスキーを貼り付け
5. 「アクティベート」をクリック
6. **期待**: 「ライセンスが有効になりました」と表示される

---

## 5. トラブルシューティング

### 5.1 「Checkout URLが取得できませんでした」

- **原因**: `create-checkout-session` が 404/401/500 を返している
- **確認**: Supabase Edge Functions の Logs を確認
- **対処**: [checkout-debugging.md](../troubleshooting/checkout-debugging.md) の「401 Unauthorized」の項を参照

### 5.2 ブラウザが開かない

- **原因**: `checkoutUrl` が null または空
- **確認**: デバッグモードで実行し、出力を確認:
  ```powershell
  dotnet run --project src\PdfHandler.UI\PdfHandler.UI.csproj
  ```
  購入ボタンクリック後、コンソールに `Checkout URL: https://...` が表示されるか確認

### 5.3 ライセンス認証が失敗する

- **原因**: `verify-license` がエラーを返している、またはネットワーク不通
- **確認**: Supabase Edge Functions → `verify-license` → Logs
- **対処**: ライセンスキーが `licenses` テーブルに存在し、`is_active = true` であることを確認

### 5.4 企業ファイアウォールでブロックされている場合

- Supabase の URL が許可されているか確認
- プロキシ環境の場合は、.NET の HttpClient がプロキシ経由でアクセスする設定が必要な場合がある

---

## 6. テスト結果の記録

| 項目 | 結果 | 備考 |
|------|------|------|
| 決済ページが開く | ✅ / ❌ | |
| Stripe 決済完了 | ✅ / ❌ | |
| ライセンスが Supabase に作成される | ✅ / ❌ | |
| ライセンスキーがアプリでアクティベートできる | ✅ / ❌ | |
| 手動ライセンスで認証できる | ✅ / ❌ | |

---

## 7. 関連ドキュメント

- [ライセンス機能テスト手順書（詳細）](./license-testing-guide.md)
- [決済・Checkout デバッグ](./../troubleshooting/checkout-debugging.md)
- [Supabase デプロイチェックリスト](./../supabase-setup/deployment-checklist.md)

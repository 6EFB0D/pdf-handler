# リリース前チェックリスト

本ドキュメントは、PDFハンドラをリリースする前に差し替え・確認が必要な項目をまとめたものです。タイミングを見て実施してください。

---

## 1. お問い合わせ先（サポート・ボリュームライセンス共通）

| 項目 | 現在の値 | 差し替え先 | ファイル |
|------|----------|------------|----------|
| お問い合わせURL | `https://example.com/contact` | 実際の問い合わせフォームURL または `mailto:xxx@example.com` | `src/PdfHandler.UI/App.xaml.cs` |

```csharp
// App.xaml.cs の ConfigureServices 内
appSettings.ContactUrl = Environment.GetEnvironmentVariable("CONTACT_URL")
    ?? "https://example.com/contact";  // ← ここを差し替え
```

**最低限**: メールアドレスのみの場合は `mailto:support@あなたのドメイン.com` を指定

---

## 2. ドキュメント内のプレースホルダー

| ファイル | 現在の値 | 内容 |
|----------|----------|------|
| `docs/enterprise/license-key-purchase-guide.md` | `enterprise@example.com` | ボリュームライセンス問い合わせメール |
| `docs/enterprise/license-key-purchase-guide.md` | `https://example.com/enterprise-contact` | 問い合わせWebフォームURL |
| `docs/enterprise/license-key-purchase-guide.md` | `03-XXXX-XXXX` | 電話番号（使用する場合） |
| `docs/user-guide/payment-guide.md` | `support@example.com` | サポートメールアドレス |

---

## 3. GitHub / リポジトリ関連

プロジェクトをフォークまたは別リポジトリで公開する場合、以下を差し替え：

| 対象 | 現在 | 検索して置換 |
|------|------|--------------|
| GitHub リポジトリURL | `https://github.com/6EFB0D/pdf-handler` | 実際のリポジトリURL |
| GitHub Issues | `https://github.com/6EFB0D/pdf-handler/issues` | 同上 |
| GitHub Discussions | `https://github.com/6EFB0D/pdf-handler/discussions` | 同上 |
| Releases API | `https://api.github.com/repos/6EFB0D/pdf-handler/releases/latest` | 同上 |

**主なファイル**:
- `src/PdfHandler.UI/Views/LicenseInfoDialog.xaml.cs`
- `src/PdfHandler.UI/Views/AboutDialog.xaml.cs`
- `src/PdfHandler.UI/Services/UpdateChecker.cs`
- `src/PdfHandler.UI/Resources/Legal/TERMS_OF_USE.txt`
- `src/PdfHandler.UI/Resources/Legal/PRIVACY_POLICY.txt`
- `src/PdfHandler.UI/Resources/Docs/USER_MANUAL.txt`

---

## 4. Supabase 関連（プロジェクト固有の場合）

別のSupabaseプロジェクトを使用する場合：

| 項目 | 設定場所 |
|------|----------|
| Supabase URL | `AppSettings.cs` の `Supabase.Url` デフォルト |
| Supabase Anon Key | `App.xaml.cs` の `SUPABASE_ANON_KEY` フォールバック |

※ 通常は環境変数で設定するため、ビルド時の差し替えは不要の場合あり

---

## 5. Stripe 関連

Supabase Edge Functions の環境変数で設定。本番用の Price ID などを確認：

- `STRIPE_PRICE_ID_PURCHASED`
- `STRIPE_PRICE_ID_SUBSCRIPTION_STANDARD`
- `STRIPE_PRICE_ID_SUBSCRIPTION_PREMIUM`
- `APP_URL`（Checkout 成功/キャンセル後のリダイレクト先）

---

## 6. 管理者用マニュアルの作成

運用担当者向けに、以下のマニュアルを作成・整備する：

| 対象 | 内容例 |
|------|--------|
| **Supabase** | ダッシュボードの使い方、ライセンステーブル・アクティベーションの確認、Edge Functions のデプロイ手順、環境変数の設定、障害時の確認手順 |
| **Stripe** | ダッシュボードの使い方、Products/Price の作成、Webhook の設定、売上・顧客の確認、返金処理 |
| **ボリュームライセンス** | ライセンスキーの手動作成・発行手順、問い合わせからの一連の流れ |
| **ライセンス管理** | アクティベーションの無効化、デバイス数の確認、トラブルシューティング |
| **問い合わせ対応** | 問い合わせの受付・振り分け、よくある質問への対応フロー |

**参考**: `docs/supabase-setup/` 配下のドキュメントをベースに、運用観点で追加・再構成する。

---

## 7. 実施タイミングの目安

| フェーズ | 実施する項目 |
|----------|--------------|
| **開発中** | 特になし（プレースホルダーのまま） |
| **テスト・ベータ** | 必要に応じて `CONTACT_URL` 環境変数で問い合わせ先を設定 |
| **リリース直前** | 上記 1〜5 の差し替えを実施 |
| **リリース後** | 問い合わせフォームやメールの運用開始 |
| **リリース前後** | 管理者用マニュアルの作成・更新 |

---

## 8. 差し替え後の確認

- [ ] 購入ダイアログで「お問い合わせ・資料請求」をクリック → 正しいURLが開く
- [ ] ライセンスキー入力で「ライセンスキーを忘れた方はこちら」→ 正しいURLが開く
- [ ] 利用規約・プライバシーポリシーのリンクが有効
- [ ] バージョンアップチェックが動作する（Releases API）

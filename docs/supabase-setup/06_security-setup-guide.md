# セキュリティ設定ガイド - 簡易版

## 今、何をすべきか（シンプルな手順）

### ✅ ステップ1: Publishable Key（公開キー）の設定

**これは公開しても安全なキーです。**

#### 方法A: コードに直接書く（開発中はOK）

`App.xaml.cs`の56行目を以下のように設定：

```csharp
appSettings.Supabase.AnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") 
    ?? "sb_publishable_ELiCbHZwAR-ekkwEvhzCcQ_mWWYB_-2"; // ここに実際のキーを書く
```

**注意**: このキーは公開しても安全ですが、GitHubにコミットする場合は問題ありません。

#### 方法B: 環境変数から読み込む（推奨）

1. Windows環境変数に設定：
   - 「システムのプロパティ」→「環境変数」→「新規」
   - 変数名: `SUPABASE_ANON_KEY`
   - 変数値: `sb_publishable_ELiCbHZwAR-ekkwEvhzCcQ_mWWYB_-2`

2. コードはそのまま（環境変数から自動的に読み込まれる）

### ✅ ステップ2: Service Role Key（機密キー）の設定

**これは絶対にコードに書いてはいけません。環境変数から読み込む必要があります。**

1. SupabaseダッシュボードでService Role Keyを取得：
   - 「Settings」→「API」→「Project API keys」
   - 「service_role」の「Reveal」をクリック
   - `eyJ`で始まるキーをコピー

2. Windows環境変数に設定：
   - 「システムのプロパティ」→「環境変数」→「新規」
   - 変数名: `SUPABASE_SERVICE_ROLE_KEY`
   - 変数値: `eyJxxxxx`（コピーしたキー）

3. コードはそのまま（環境変数から自動的に読み込まれる）

**重要**: Service Role Keyはコードに書かないでください。環境変数のみで設定してください。

## まとめ

### 今すぐやること

1. **Publishable Key**: コードの56行目のフォールバック値を実際のキーに更新
   - または、環境変数`SUPABASE_ANON_KEY`を設定

2. **Service Role Key**: 環境変数`SUPABASE_SERVICE_ROLE_KEY`を設定
   - コードには書かない

3. **動作確認**: アプリケーションを起動して、Supabaseに接続できるか確認

## セキュリティチェックリスト

- ✅ Publishable Keyはコードに書いてもOK（公開しても安全）
- ✅ Service Role Keyは環境変数のみ（コードに書かない）
- ✅ GitHubにコミットする前に、Service Role Keyがコードに含まれていないか確認
- ✅ `.gitignore`に`.env`ファイルが含まれているか確認




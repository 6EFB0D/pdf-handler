# Supabaseキーの確認方法

## キーの種類と形式

Supabaseダッシュボードには2種類のキーが表示されます：

### 1. `anon` key（公開キー）
- **形式**: `eyJ` で始まるJWT形式、または `sb_publishable_` で始まる新しい形式
- **用途**: クライアント側（WPFアプリ）で使用
- **セキュリティ**: 公開しても安全（RLSポリシーで保護）

### 2. `service_role` key（機密キー）
- **形式**: 常に `eyJ` で始まるJWT形式
- **用途**: サーバーサイド（Edge Functions）のみ
- **セキュリティ**: 機密情報、絶対に公開しない

## 確認手順

1. Supabaseダッシュボードにアクセス
   - https://supabase.com/dashboard/project/yzmjuotvkxcfnsgleyxl/settings/api

2. 「Project API keys」セクションを確認

3. 以下の2つのキーを確認：
   - **anon key**: `eyJ` または `sb_publishable_` で始まる
   - **service_role key**: `eyJ` で始まる（別のキー）

## 正しいキーの設定方法

### 方法1: 環境変数に設定（推奨）

1. Windows環境変数に設定：
   ```
   変数名: SUPABASE_ANON_KEY
   変数値: eyJxxxxx（anon keyの実際の値）
   ```

2. コードはそのまま（環境変数から自動的に読み込まれる）

### 方法2: コードに直接書く（開発中のみ）

`App.xaml.cs`の56行目を更新：

```csharp
appSettings.Supabase.AnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") 
    ?? "eyJxxxxx"; // 実際のanon keyに置き換え
```

## 注意事項

- `anon` keyと`service_role` keyは**別のキー**です
- `anon` keyは公開しても安全です
- `service_role` keyは絶対にコードに書かないでください




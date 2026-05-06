# ライセンス管理機能のデプロイ手順

ライセンス管理ダイアログ（デバイス一覧・解除・表示名編集）を有効にするには、以下の手順を実行してください。

## 1. データベースマイグレーション

Supabase SQL Editor で以下を実行:

```
docs/supabase-setup/09_add-device-name-and-display-name.sql
```

- `license_activations` に `device_name` と `display_name` カラムを追加します。

## 2. Edge Functions のデプロイ

以下の3つの Edge Function をデプロイ:

```bash
supabase functions deploy get-activations
supabase functions deploy deactivate-device
supabase functions deploy update-device-display-name
```

既存の `verify-license` も更新されているため、あわせてデプロイ:

```bash
supabase functions deploy verify-license
```

## 3. アプリの利用方法

1. ライセンス有効時に「ヘルプ」→「バージョン情報」を開く
2. 「⚙ ライセンス管理」ボタンが表示される
3. クリックで登録デバイス一覧を表示
4. 各デバイスで「名前を編集」「解除」が可能

## 補足

- デバイス数は 3台/ライセンス のまま
- `device_name`: アクティベーション時の `Environment.MachineName`（自動）
- `display_name`: ユーザーが「名前を編集」で設定可能
- 既存レコードは `device_name` が NULL の場合、「デバイス1」「デバイス2」等で表示

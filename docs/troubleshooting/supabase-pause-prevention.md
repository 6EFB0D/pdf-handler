# Supabase 7日間一時停止の回避

## 概要

Supabase の無料プランでは、**7日間アクティビティがない**とプロジェクトが一時停止します。以下で回避できます。

---

## 1. 今すぐ実行（一時停止を防ぐ）

### 方法A: ping Edge Function をデプロイして呼び出す

```powershell
# プロジェクトディレクトリで実行
cd D:\Users\admin_mak\project\pdf-handler

# ping をデプロイ
npx supabase functions deploy ping --project-ref yzmjuotvkxcfnsgleyxl --no-verify-jwt

# 手動で ping を呼び出し（アクティビティを記録）
curl "https://yzmjuotvkxcfnsgleyxl.supabase.co/functions/v1/ping"
```

### 方法B: ダッシュボードから復帰

既に一時停止している場合:

1. [Supabase ダッシュボード](https://supabase.com/dashboard) にログイン
2. プロジェクトを選択
3. 「Restore project」または「Resume」をクリック

---

## 2. 自動 Heartbeat（GitHub Actions）

`.github/workflows/supabase-heartbeat.yml` が設定済みです。

- **スケジュール**: 毎日 0:00 UTC（日本時間 9:00）
- **処理**: `ping` Edge Function を呼び出し
- **手動実行**: GitHub → Actions → Supabase Heartbeat → Run workflow

### 初回セットアップ

1. **ping をデプロイ**（上記の方法A、または `.\scripts\deploy-supabase-functions.ps1`）
2. **GitHub にプッシュ**してワークフローを有効化
3. 初回は手動で「Run workflow」を実行して動作確認

---

## 3. 注意事項

- GitHub Actions は **デフォルトブランチ** のワークフローがスケジュール実行されます
- リポジトリが **private** の場合、GitHub Actions の無料枠に制限があります
- プロジェクトリファレンスを変更した場合は、ワークフロー内の URL を更新してください

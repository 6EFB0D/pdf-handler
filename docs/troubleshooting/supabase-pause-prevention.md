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

> **2026-04-25 移管**: 本ハートビートは `pdf-handler` リポジトリから **`6EFB0D/backoffice`** リポジトリへ移動しました。`pdf-handler/.github/workflows/supabase-heartbeat.yml` は**削除済み**で、現在の `pdf-handler/.github/workflows/` は `track-downloads.yml` のみです。

- **設置先リポジトリ**: `github.com/6EFB0D/backoffice`（運用ジョブ集約）
- **ワークフロー名**: `supabase-heartbeat.yml`（同名）
- **スケジュール**: 毎日 0:00 UTC（日本時間 9:00）
- **処理**: `ping` Edge Function を呼び出し（`https://yzmjuotvkxcfnsgleyxl.supabase.co/functions/v1/ping`）
- **手動実行**: GitHub → backoffice → Actions → Supabase Heartbeat → Run workflow
- **必要な Repository Secrets**:
  - `SUPABASE_FUNCTIONS_URL`（任意・URL を Secrets で隠す場合）
  - 認証不要（`--no-verify-jwt` でデプロイされている `ping` 関数は AnonKey も不要）

### 移管理由

- **pdf-handler** のリポジトリは**製品開発の CI／リリース**に集中させる
- **backoffice** に**運用ジョブ**（ハートビート、月次集計、退会処理等）を集約することで、運用変更が製品リポジトリの履歴を汚染しない
- **資産台帳**（`legals/security-compliance/Security checklist for Credit/PHASE_4_1_REPO_ASSET_INVENTORY.md` 行 8）に登録済み

### 初回セットアップ（再実行時の手順）

1. **ping をデプロイ**（pdf-handler 側で実施）:
   ```powershell
   cd D:\Users\admin_mak\project\pdf-handler
   npx supabase functions deploy ping --project-ref yzmjuotvkxcfnsgleyxl --no-verify-jwt
   ```
2. **backoffice にプッシュ**してワークフローを有効化
3. 初回は backoffice の「Run workflow」を手動実行して動作確認

---

## 3. 注意事項

- GitHub Actions は **デフォルトブランチ** のワークフローがスケジュール実行されます
- リポジトリが **private** の場合、GitHub Actions の無料枠に制限があります
- プロジェクトリファレンスを変更した場合は、**backoffice** 側のワークフロー内 URL を更新してください
- **ハートビートが 7 日以上失敗**するとプロジェクトが一時停止する可能性があるため、backoffice 側で **失敗通知**（Issue 起票・メール）を設定することを推奨

# リリースダウンロード統計の移行計画（Automations + Supabase）

## 背景・課題

| 課題 | 説明 |
|------|------|
| ローカル実行 | PC スリープ・オフラインで夜間ジョブが失敗しやすい |
| 公開リポジトリへのコミット | `stats/*.json` を `main` に積むと履歴が肥大化し、公開範囲とも相性が悪い |
| バッジ | 公開 README は **Shields の GitHub 標準バッジ**（`github/downloads`）で十分。独自 `endpoint` 用 JSON は不要 |

## 目標

1. **集計ジョブは GitHub 上のスケジュール実行**（開発者 PC に依存しない）。
2. **対象リポジトリ**は本番の公開先（例: `6EFB0D/pdf-handler`）。新しい Release を出しても **同一 API** で取得できるため、**リリースのたびにワークフロー変更は原則不要**。
3. **履歴データ**は **Supabase のテーブル**に日次（または 1 日 1 回のスナップショット）で保存。必要ならダッシュボードや社内レポートから参照。
4. **公開 `pdf-handler` の README** は現状どおり Shields 標準のみ（外部から GitHub API を読む方式）。

## アーキテクチャ案

```
┌─────────────────────────────────────┐
│ Automations リポジトリ               │  ← 例: backoffice / pdf-handler-automations
│  .github/workflows/download-stats.yml │
│  on: schedule (cron UTC)             │
└──────────────┬──────────────────────┘
               │ GitHub REST API
               │ GET /repos/{owner}/{repo}/releases
               ▼
┌─────────────────────────────────────┐
│ 集計スクリプト（bash + jq 等）        │
│ ・合計ダウンロード数                  │
│ ・リリース別・Asset 別（任意）         │
└──────────────┬──────────────────────┘
               │ HTTPS + service_role または専用 Edge Function
               ▼
┌─────────────────────────────────────┐
│ Supabase (Postgres)                 │
│ テーブル: release_download_snapshots │
└─────────────────────────────────────┘
```

- **ジョブの置き場所**: 既存方針どおり **製品リポジトリ以外**（例: `6EFB0D/backoffice`）にワークフローを追加するのがよい。`pdf-handler` 公開リポにはワークフローを置かない。
- **認証**: 対象が **公開リポジトリ**なら API は未認証でも読めるが、**レート制限**回避のため **Fine-grained PAT** または **GitHub App** を Automations リポの **Secrets** に保存するのを推奨。
- **Supabase 書き込み**: Actions から `POST /rest/v1/<table>` に `apikey` + `Authorization: Bearer <SERVICE_ROLE_KEY>`（**リポジトリ Secrets** のみ。ログに出さない）。または **専用 Edge Function**（共有秘密ヘッダー 1 本）で insert のみ許可し、キーをローテーションしやすくする。

## Supabase スキーマ案（初期）

```sql
-- 1 リポジトリ・1 暦日あたり 1 行（日次スナップショット）
create table if not exists public.release_download_stats_daily (
  id uuid primary key default gen_random_uuid(),
  repo_full_name text not null,
  stats_date date not null,
  total_downloads bigint not null,
  releases_json jsonb not null,
  created_at timestamptz not null default now(),
  unique (repo_full_name, stats_date)
);

-- RLS: anon/authenticated からは select も不可が基本。運用は service_role のみ。
alter table public.release_download_stats_daily enable row level security;
-- 必要に応じて「社内 read-only 用」policy は別途
```

- `releases_json`: 旧 `stats/releases-summary-*.json` に相当する配列をそのまま保存しておくと、後から Asset 別の追跡が容易。
- さらに **生ログ**が必要なら `raw_api_response jsonb` 列を追加してもよい（サイズ注意）。

## ワークフロー実装タスク（Automations 側）

1. [ ] `download-stats.yml` を追加（`schedule` + `workflow_dispatch`）。
2. [ ] `GH_TOKEN`（または `PDF_HANDLER_STATS_PAT`）を Secrets に登録。
3. [ ] `SUPABASE_URL` / `SUPABASE_SERVICE_ROLE_KEY`（または Edge Function URL + `STATS_INGEST_SECRET`）を Secrets に登録。
4. [ ] 上記 SQL を **DEV → PROD** の順で Supabase に適用。
5. [ ] 初回手動実行で 200 応答とテーブル行を確認。
6. [ ] アラート（失敗時 Slack / メール）は任意。

## 公開 `pdf-handler` 側（本リポジトリ）

- [x] `track-downloads.yml` 削除済み、`stats/` は `.gitignore`。
- [x] README のバッジは `img.shields.io/github/downloads/...` のみ維持。
- 本ドキュメントは **メンテナ向け計画**。実装・Secrets の値は **DEV / Automations リポジトリ**で管理。

## オプション: Git と併用

- 「Git にもミラーしたい」場合は、Automations で **別ブランチ**や **gist**、または **運用リポジトリの private ブランチ**に JSON をコミットする方式も可能。**pdf-handler の `main` は汚さない**方針を推奨。

## 参照

- [GitHub Releases API](https://docs.github.com/rest/releases/releases)
- [Shields: GitHub downloads](https://shields.io/badges/github-downloads-all)

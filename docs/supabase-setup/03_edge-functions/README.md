# Edge Functions 参照コピー（`docs/supabase-setup/03_edge-functions`）

## 正（ソース・オブ・トゥルース）

**デプロイは常にリポジトリルートの `pdf-handler/supabase/functions/` から行ってください。**

- `npx supabase functions deploy <名前> --project-ref <DEV または PROD> --no-verify-jwt`
- 作業ディレクトリ例: `cd D:\Users\admin_mak\project\pdf-handler`
- `admin-generate-license` / `verify-license` / `stripe-webhook` は **`_shared/compact-license-key.ts` を相対 import** している。CLI がバンドル時に同梱する（`supabase/functions` 側の-tree を使うこと）。

## このフォルダの役割

- **DEV / PROD 設計は同一**。違いは Supabase プロジェクト（`--project-ref`）と Secrets のみ。
- 本ディレクトリはドキュメント・レビュー用に、主要ファイルを **`supabase/functions` から同期**したスナップショットです。手元で差分確認する用途向けで、**ここだけをデプロイ根にしない**でください。

## 同期済みファイル（目安）

| パス | 元 |
|------|-----|
| `_shared/compact-license-key.ts` | `supabase/functions/_shared/` |
| `verify-license/index.ts` | `supabase/functions/verify-license/` |
| `stripe-webhook/index.ts` | `supabase/functions/stripe-webhook/` |

その他の関数（`create-checkout-session` 等）は必要に応じて `supabase/functions` を参照してください。

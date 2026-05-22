# ドキュメント（公開リポジトリ）

このフォルダにはエンドユーザー向けと、開発者向けの**最小限**の資料だけを残しています。

- **ユーザー向け**: [`user-guide/`](user-guide/)（ライセンス購入ガイドなど）
- **リリースノート例**: [`public/`](public/)
- **GitHub Pages 用 HTML（チェックアウト完了画面など）**: [`github-pages/`](github-pages/)

次のような内容は**公開しない**運用です。詳細・手順は開発用リポジトリ（フォルダ名・リポジトリ名に `DEV` が付いたもの）に置いてください。

- Supabase／Stripe／メール運用などのセットアップ手順
- 内部向けチェックリスト、マイグレーション SQL、アーキテクチャとコンプライアンス根拠（長文・証跡類）
- 旧来のローカル／リポジトリ直コミット型の統計・アーカイブ本体

メンテナ向けの**設計メモのみ**は [`maintainer/download-stats-automation-plan.md`](maintainer/download-stats-automation-plan.md)（日次ダウンロードを Automations + Supabase に移す計画）として置いています。実コード・Secrets・マイグレーションの実行は Automations／DEV 側で行ってください。

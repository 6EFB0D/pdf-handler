# GitHubリポジトリ構成の推奨事項

## 推奨構成

### 開発用リポジトリ（Private）
- **目的**: 開発・テスト・内部管理用
- **内容**:
  - ソースコード全体
  - Supabase Edge Functionsのコード
  - データベーススキーマ（SQLファイル）
  - 設定ファイル（Secretsの値は含めない）
  - 開発ドキュメント
  - テストコード

### 公開用リポジトリ（Public）
- **目的**: オープンソース化・コミュニティ参加
- **内容**:
  - ソースコード（機密情報を除く）
  - ライセンスファイル
  - README.md
  - ユーザー向けドキュメント
  - コントリビューションガイド

## 現時点での進め方

### ✅ このまま進めて問題ありません

理由：
1. **Edge Functionsのコードは `supabase/functions/` に集約**
   - **デプロイの正**: `pdf-handler/supabase/functions/`（`_shared` 含む。コンパクトライセンスキー実装と一致）
   - `docs/supabase-setup/03_edge-functions/` は参照用コピー（README 参照）

2. **GitHubリポジトリの構成は後で決められる**
   - セットアップ完了後にリポジトリを作成しても問題ない
   - Edge Functionsのコードは既にプロジェクト内にあるので、そのままリポジトリに含められる

3. **Secretsの管理**
   - Stripeのシークレットキーなどは、GitHubリポジトリには含めない
   - SupabaseのSecretsとして管理（既にその方針）

## セットアップ完了後の推奨アクション

1. **開発用リポジトリ（Private）を作成**
   ```bash
   # プロジェクトルートで
   git init
   git add .
   git commit -m "Initial commit"
   git remote add origin https://github.com/your-username/pdf-handler-dev.git
   git push -u origin main
   ```

2. **.gitignoreの確認**
   - Secretsファイル（`.env`など）が除外されていることを確認
   - ビルド成果物（`bin/`, `obj/`）が除外されていることを確認

3. **公開用リポジトリの準備**
   - 開発用リポジトリから、機密情報を除いたバージョンを作成
   - または、公開用ブランチを作成して管理

## Edge Functionsのデプロイについて

### 現状
- **デプロイ元ディレクトリ**: `supabase/functions/`（プロジェクトルート直下）
- `docs/supabase-setup/03_edge-functions/` はドキュメント用スナップショット（DEV/PROD 同一設計の説明用）

### 将来的な管理
- Edge Functionsのコードも開発用リポジトリに含める
- デプロイは、リポジトリから直接実行可能
- CI/CDパイプラインを設定する場合も、リポジトリからデプロイ可能

## 推奨事項まとめ

1. **現時点**: このままセットアップを進める ✅
2. **セットアップ完了後**: 開発用リポジトリ（Private）を作成
3. **将来的に**: 公開用リポジトリ（Public）を作成（必要に応じて）

現在の構成で問題なく進められます。




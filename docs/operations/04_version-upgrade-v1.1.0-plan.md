# 4. v1.1.0 アップグレード計画（バックアップ・ライセンス・価格変更）

ライセンス管理・価格設定の変更と v1.1.0 へのバージョンアップを行う際の実施計画です。  
v1.0.0 を安全にバックアップし、顧客サポート・バグ再現試験に備えます。

---

## 前提・決定事項（v1.1.0 骨格）

| 項目 | 内容 |
|------|------|
| **既存購入者** | なし（Stripe はサンドボックスのみ運用） |
| **ライセンス形式** | `PDFH-P101-{28文字}-{HMAC}`（[license-code-specification.md](../specs/license-code-specification.md) 参照） |
| **app_id** | `PDFH` 固定 |
| **Supabase スキーマ** | 既存を維持 |
| **価格** | ¥3,850（税込）。税込であることを明示 |
| **キー表示** | メール・UI・コピペで同一フォーマット（4桁区切り）例: `PDFH-P101-A1B2-C3D4-E5F6-G7H8-I9J0-K1L2-M3` |

---

## v1.1.0 の主な変更内容（2点）

### 1. 廃業時にもユーザーに継続利用してもらうためのライセンス認証方式の変更

- **現状**: Supabase へのオンライン検証のみ → サービス終了時にユーザーが使えなくなる
- **変更後**: HMACハイブリッド方式
  - 起動時はオンライン検証（5秒タイムアウト）
  - 接続不可時は HMAC オフライン検証にフォールバック（7日間有効）
  - 撤退時は最終パッチでオフライン永続化により継続利用可能

**関連仕様**: [license_management_hmac_hybrid.md](../specs/license_management_hmac_hybrid.md)

### 2. 消費税の取り扱いと表示方法の適正化

- **現状**: 価格表示・請求書の書き方が未整備の可能性
- **変更後**:
  - 一般消費者向け: 税込価格のみ表示（総額表示義務対応）
  - 免税事業者: Stripe は税込単一価格で設定
  - 「消費税相当額」としての別表記ルールの遵守

**関連仕様**: [app_dev_consumption_tax.md](../specs/app_dev_consumption_tax.md)

---

## 全体フロー

```
Phase 1: v1.0.0 バックアップ
    ↓
Phase 2: バージョン v1.1.0 への変更（csproj, USER_MANUAL）
    ↓
Phase 3: HMACハイブリッドライセンス認証の実装
    ↓
Phase 4: 消費税の取り扱い・表示の適正化
    ↓
Phase 5: Supabase レコードクリーンアップ（必要に応じて）
    ↓
Phase 6: Stripe 価格設定の変更
    ↓
Phase 7: ライセンス管理方針・ドキュメントの反映
    ↓
Phase 8: サポート・お問い合わせメニューと GitHub 商品紹介ページ
```

---

## Phase 1: v1.0.0 バックアップ

### 1.1 Git タグの作成

v1.0.0 の現在の状態を永続的に保存します。

```powershell
cd d:\Users\admin_mak\project\pdf-handler

# コミットしてから実行（未コミットの変更がある場合）
git status
git add -A
git commit -m "fix: 試用期間終了時の購入ダイアログ繰り返し表示を修正"

# v1.0.0 タグを作成（現在のコミットを指す）
git tag -a v1.0.0 -m "PDF Handler v1.0.0 - 安定版バックアップ"

# リモートにプッシュ
git push origin v1.0.0
```

**確認**: GitHub の Releases または Tags ページで `v1.0.0` が表示されること。

### 1.2 v1.0.0 ビルド成果物の保存

バグ再現や顧客サポート用に、v1.0.0 の実行ファイルを保存します。

**スクリプトを使う場合（推奨）:**

```powershell
# 1.1 でタグを作成した後
.\scripts\backup-v1.0.0.ps1
```

**手動で実行する場合:**

```powershell
# 1. v1.0.0 のタグにチェックアウト
git checkout v1.0.0

# 2. ビルド
.\scripts\build.ps1 -Release

# 3. ビルド成果物をバックアップフォルダにコピー
$backupDir = ".\backups\v1.0.0"
New-Item -ItemType Directory -Force -Path $backupDir
Copy-Item ".\src\PdfHandler.UI\bin\Release\net8.0-windows\*" -Destination $backupDir -Recurse

# 4. バージョン情報を記録（上記スクリプトが自動で行う）
# 5. main または作業ブランチに戻る
git checkout main   # または feature/v1.1-page-operations など
```

**保存場所**: `backups/v1.0.0/`（`.gitignore` で除外。リポジトリにはコミットしない）  
- `PdfHandler.UI.exe` および依存DLL  
- `version-info.txt`（ビルド番号など）

### 1.3 GitHub Release の作成（推奨）

GitHub 上で v1.0.0 の Release を作成し、ダウンロード可能にします。

1. GitHub リポジトリ → **Releases** → **Create a new release**
2. **Tag**: `v1.0.0` を選択
3. **Title**: `v1.0.0`
4. **Description**: リリースノート（変更内容のサマリ）
5. **Attach**: ビルドした `PdfHandler-v1.0.0-win-x64.zip` などをアップロード
6. **Publish release**

---

## Phase 2: バージョン v1.1.0 への変更

### 2.1 変更対象ファイル

| ファイル | 変更内容 |
|----------|----------|
| `src/PdfHandler.UI/PdfHandler.UI.csproj` | Version, FileVersion, AssemblyVersion → 1.1.0 |
| `src/PdfHandler.Infrastructure/PdfHandler.Infrastructure.csproj` | 同上 |
| `src/PdfHandler.UI/Resources/Docs/USER_MANUAL.txt` | バージョン表記 → 1.1.0 |

### 2.2 変更例（csproj）

```xml
<Version>1.1.0</Version>
<FileVersion>1.1.0.0</FileVersion>
<AssemblyVersion>1.1.0.0</AssemblyVersion>
```

※ AssemblyVersion を 1.0.0.0 のままにして API 互換性を維持する方法もあるが、バージョン表示が変わるため 1.1.0.0 に合わせることを推奨。

---

## Phase 3: HMACハイブリッドライセンス認証の実装

廃業時にもユーザーが継続利用できるよう、ライセンス認証方式を変更します。

### 3.1 参照仕様

- [license_management_hmac_hybrid.md](../specs/license_management_hmac_hybrid.md)

### 3.2 実装タスク（概要）

| 項目 | 内容 |
|------|------|
| **Supabase** | 既存スキーマ維持。`app_id` は `PDFH` 固定 |
| **ライセンスキー形式** | `PDFH-P101-{28文字}-{HMAC}`（[license-code-specification.md](../specs/license-code-specification.md)） |
| **キー表示** | 4桁区切りで統一（メール・UI・コピペ）例: `PDFH-P101-A1B2-C3D4-E5F6-G7H8-I9J0-K1L2-M3` |
| **stripe-webhook** | 新形式ライセンスキーの発行ロジック（HMAC署名付き） |
| **verify-license** | オンライン検証 + オフライン検証のフォールバック |
| **アプリ側** | 5秒タイムアウト、7日間キャッシュ、オフライン時の HMAC 検証 |
| **環境変数** | `LICENSE_SECRET_KEY` の追加 |

### 3.3 既存ライセンスとの互換性

- 既存購入者はなし（Stripe サンドボックスのみ）のため、移行考慮は不要

---

## Phase 4: 消費税の取り扱い・表示の適正化

### 4.1 参照仕様

- [app_dev_consumption_tax.md](../specs/app_dev_consumption_tax.md)

### 4.2 実装タスク（概要）

| 項目 | 内容 |
|------|------|
| **Stripe** | 税込価格の単一設定（税率設定は使わない） |
| **価格** | ¥3,850（税込） |
| **アプリ内の価格表示** | 「¥3,850（税込）」など税込である旨を明示 |
| **領収証・請求書** | 税込価格のみ表示、または「消費税相当額」として別表記。登録番号は記載しない |
| **ドキュメント** | 運用マニュアルに消費税取り扱いを追記 |

---

## Phase 5: Supabase レコードクリーンアップ

**注意**: 本番データを削除する前に、必ずバックアップを取得してください。

### 5.1 バックアップ取得

1. Supabase ダッシュボード → **Database** → **Backups**
2. 手動バックアップを作成するか、既存の日次バックアップを確認

### 5.2 クリーンアップの検討

| 対象 | 目的 | 注意 |
|------|------|------|
| `licenses` | テスト用レコードの削除 | 本番顧客データは残す |
| `license_activations` | 同上 | CASCADE で licenses 削除時に連動 |
| `subscriptions` | 同上 | CASCADE で licenses 削除時に連動 |

### 5.3 クリーンアップ用 SQL 例（テスト環境向け）

```sql
-- テスト用メールアドレスのライセンスのみ削除（例）
-- 実行前に必ず対象を確認すること
DELETE FROM licenses 
WHERE user_email LIKE '%test%' 
   OR user_email LIKE '%example%'
   OR user_email = 'your-test-email@example.com';
```

**全件削除**（開発・テスト環境のみ。本番では絶対に実行しない）:

```sql
-- 外部キー制約のため、activations と subscriptions から先に削除
TRUNCATE license_activations CASCADE;
TRUNCATE subscriptions CASCADE;
TRUNCATE licenses CASCADE;
```

### 5.4 スキーマ変更（必要な場合）

ライセンス管理方針変更に伴い、テーブル構造を変更する場合は新しいマイグレーションファイルを作成します。

- 例: `docs/supabase-setup/10_xxx.sql`

---

## Phase 6: Stripe 価格設定の変更

### 6.1 変更方針の確認

- 既存の Price ID を**非表示**にして新規 Price を作成するか
- 同じ Price ID のまま金額のみ変更するか（Stripe では推奨されない場合あり）

### 6.2 新規 Products & Prices の作成

1. Stripe ダッシュボード → **Products** → **Add product**
2. 新しい価格で Product / Price を作成
3. **Price ID** をコピー

### 6.3 Supabase 環境変数の更新

Supabase Edge Functions の Secrets を更新:

| 変数 | 新しい値 |
|------|----------|
| `STRIPE_PRICE_ID_PURCHASED` | 新しい Price ID |
| `STRIPE_PRICE_ID_SUBSCRIPTION_STANDARD` | 新しい Price ID |
| `STRIPE_PRICE_ID_SUBSCRIPTION_PREMIUM` | （変更する場合） |

### 6.4 Edge Functions の再デプロイ

```powershell
.\scripts\deploy-supabase-functions.ps1
# または
npx supabase functions deploy create-checkout-session --project-ref <your-project-ref>
npx supabase functions deploy stripe-webhook --project-ref <your-project-ref> --no-verify-jwt
```

---

## Phase 7: ライセンス管理方針・ドキュメントの反映

価格変更・ライセンス管理の変更内容に応じて、以下を更新します。

- `docs/operations/` 内の運用ドキュメント
- アプリ内の料金表示（`LicenseDialog.xaml` など）※ ¥3,850（税込）で統一
- `docs/specs/license-code-specification.md`（HMAC 新形式の反映）
- `docs/supabase-setup/04_setup-instructions.md` の Stripe 価格記載

---

## Phase 8: サポート・お問い合わせメニューと GitHub 商品紹介ページ

PictComp と同様に、ヘルプメニュー内に「サポート・お問い合わせ」サブメニューを追加し、ユーザーが簡単に問い合わせ・フィードバックできるようにします。

### 8.1 ヘルプメニュー構成（サポート・お問い合わせ サブメニュー）

| メニュー項目 | 遷移先 |
|--------------|--------|
| **PDF Handlerのページ** | GitHub 商品紹介ページ（後述） |
| **お問い合わせ（support@xxx）** | `ContactUrl`（既存の `CONTACT_URL`） |
| （区切り線） | |
| **アンケート・要望を送信** | Google フォーム（アンケート・要望フォーム） |

### 8.2 GitHub 商品紹介ページの作成・追加

- リポジトリに商品紹介ページを追加する
- 例: `docs/product.md` または GitHub Pages で専用サイト
- 機能紹介・価格・ダウンロードリンクなどを掲載
- `ProductPageUrl` に設定（環境変数 `PRODUCT_PAGE_URL` で上書き可能）

### 8.3 アンケート・要望フォーム

- Google フォームで PDF Handler 用のアンケート・要望フォームを作成
- 回答用 URL は `/viewform` 形式（例: `https://docs.google.com/forms/d/{id}/viewform`）
- アプリ内メニューから開く
- `SurveyFormUrl` に設定（環境変数 `SURVEY_FORM_URL` で上書き可能）

### 8.4 実装タスク

| 項目 | 内容 |
|------|------|
| **AppSettings** | `ProductPageUrl`、`SurveyFormUrl` の追加 |
| **MainWindow.xaml** | ヘルプメニューに「サポート・お問い合わせ」サブメニューを追加 |
| **MainWindow.xaml.cs** | 各メニュー項目のクリックハンドラ（URL をブラウザで開く） |
| **ドキュメント** | `USER_MANUAL.txt` にサポート・お問い合わせの説明を追記 |

---

## 実施チェックリスト

### v1.0.0 バックアップ
- [ ] Git タグ `v1.0.0` を作成
- [ ] タグをリモートにプッシュ
- [ ] v1.0.0 でビルドし `backups/v1.0.0/` に保存
- [ ] GitHub Release v1.0.0 を作成（任意）

### バージョン変更
- [ ] `PdfHandler.UI.csproj` を 1.1.0 に更新
- [ ] `PdfHandler.Infrastructure.csproj` を 1.1.0 に更新
- [ ] `USER_MANUAL.txt` を 1.1.0 に更新

### HMACハイブリッドライセンス（Phase 3）
- [ ] Supabase テーブル・スキーマの見直し
- [ ] ライセンスキー新形式の設計・既存互換性の検討
- [ ] stripe-webhook に HMAC 発行ロジックを実装
- [ ] verify-license にオフライン検証フォールバックを実装
- [ ] アプリ側に 5秒タイムアウト・7日キャッシュ・HMAC 検証を実装
- [ ] `LICENSE_SECRET_KEY` 環境変数を設定

### 消費税の適正化（Phase 4）
- [ ] Stripe を税込単一価格で設定
- [ ] アプリ内の価格表示を「税込」に統一
- [ ] 領収証・請求書の表示ルールを整備

### Supabase（Phase 5）
- [ ] バックアップを取得
- [ ] レコードクリーンアップ（対象を特定して実行）
- [ ] スキーマ変更がある場合はマイグレーション作成・実行

### Stripe（Phase 6）
- [ ] 新価格で Products & Prices を作成
- [ ] Supabase Secrets を更新
- [ ] Edge Functions を再デプロイ

### ドキュメント・UI（Phase 7）
- [ ] 料金表示を更新
- [ ] 運用ドキュメントを更新
- [ ] license-code-specification.md に HMAC 新形式を反映

### サポート・お問い合わせ（Phase 8）
- [ ] GitHub 商品紹介ページを作成・追加
- [ ] PDF Handler 用アンケート・要望 Google フォームを作成
- [ ] ヘルプメニューに「サポート・お問い合わせ」サブメニューを実装
- [ ] `ProductPageUrl` / `SurveyFormUrl` を環境変数またはコードで設定

---

## 参考リンク

- [Git タグの作成](https://git-scm.com/book/ja/v2/Git-%E3%81%AE%E5%9F%BA%E6%9C%AC-%E3%82%BF%E3%82%B0)
- [Stripe Products & Prices](https://dashboard.stripe.com/products)
- [Supabase Backups](https://supabase.com/docs/guides/platform/backups)

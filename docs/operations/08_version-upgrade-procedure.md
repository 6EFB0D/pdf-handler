# メジャーバージョンアップ手順

> 対象: pdfHandler（PDFH）および将来の追加アプリ  
> バージョン管理方針: Stripe 商品メタデータ `major_version` で購入バージョンを記録  
> 作成: 2026-04-19

---

## バージョン管理の設計思想

```
Stripe 商品（Product）
  └─ メタデータ: major_version = "1"
          │
          ▼ 購入時 (checkout.session.completed)
Supabase licenses テーブル
  └─ purchased_version = "1"  ← 購入時点のバージョンが永続記録される
          │
          ▼ 認証時
pdfHandler アプリ
  └─ 自分のバージョンと purchased_version を比較して
     「このライセンスで使えるか」を判断
```

- **purchased_version は変更しない**: 購入した事実の記録なので、バージョンアップ後も過去の値を保持する
- **v1 購入者が v2 を使いたい場合**: 差額アップグレード購入 or 新規購入を促す
- **v1 購入者が v1 を使い続ける場合**: 既存ライセンスをそのまま使用できる

---

## Step 1: アプリ（C#）の改修

### 1-1. バージョン定数を更新

```csharp
// src/PdfHandler.Core/Constants/AppConstants.cs（または類似ファイル）
public const string MajorVersion = "2";  // "1" → "2" に変更
```

### 1-2. ライセンス検証ロジックの確認

`verify-license` の応答に `purchasedVersion` が含まれる場合、アプリ側で比較処理を追加：

```csharp
// LicenseService.cs の検証結果処理
if (result.PurchasedVersion != null &&
    int.Parse(result.PurchasedVersion) < int.Parse(AppConstants.MajorVersion))
{
    // v1 ライセンスで v2 を使おうとしている
    ShowUpgradeRequiredDialog();
    return;
}
```

### 1-3. ビルド・インストーラー更新

- バージョン番号をインストーラーに反映
- リリースノートを `docs/release/` に追加

---

## Step 2: Stripe の設定

### 2-1. 新しい商品（Product）を作成

```
Stripe Dashboard → 商品カタログ → 商品を追加
  商品名: pdfHandler Standard v2
  価格: （v2 の価格を設定）
  メタデータ:
    plan          StandardPurchased
    app_id        PDFH
    major_version 2                ← ここが重要
```

> ⚠️ 既存の v1 商品はそのまま残す（v1 購入者への継続サポートのため）

### 2-2. Payment Link を更新（使用している場合）

```
Stripe Dashboard → Payment Links → 新しいリンクを作成
  商品: pdfHandler Standard v2 を選択
```

旧 Payment Link は v1 購入者向けページ等に残してもよい。

### 2-3. 差額アップグレード商品（任意）

```
商品名: pdfHandler v1 → v2 アップグレード
価格: （差額）
メタデータ:
  plan           StandardPurchased
  app_id         PDFH
  major_version  2
  upgrade_from   1
```

---

## Step 3: Supabase の確認・対応

### 3-1. スキーマ変更（通常は不要）

`purchased_version` 列はすでに TEXT 型のため、値として `"2"` を格納できる。  
**スキーマ変更は不要**。

### 3-2. Edge Functions の確認

`stripe-webhook/index.ts` は Stripe メタデータから `major_version` を自動取得するため、  
**Edge Function の変更は不要**。

```typescript
// 現在の実装（自動対応済み）
const majorVer = session.metadata?.major_version
  ?? Deno.env.get("LICENSE_PURCHASED_MAJOR_VERSION")
  ?? "1";
```

### 3-3. 環境変数（フォールバック用）の更新（任意）

Stripe メタデータ未設定時のフォールバックを変えたい場合のみ：

```bash
npx supabase secrets set LICENSE_PURCHASED_MAJOR_VERSION=2 \
  --project-ref yzmjuotvkxcfnsgleyxl
```

> 通常は不要。Stripe メタデータが正しく設定されていれば環境変数は参照されない。

---

## Step 4: 動作確認

### 4-1. Stripe テストモードで v2 商品を購入

```
テストカード: 4242 4242 4242 4242
→ purchased_version = "2" で licenses に登録されることを確認
```

確認 SQL:

```sql
SELECT license_key, purchased_version, created_at
FROM licenses
ORDER BY created_at DESC
LIMIT 3;
```

### 4-2. v1 ライセンスでの動作確認

既存の v1 ライセンスで v2 アプリを起動し、  
アップグレード案内が表示されることを確認（アプリ側実装次第）。

### 4-3. support@office-goplan.com への通知メール確認

購入完了通知メールに `purchased_version: 2` が記録されていることを確認。

---

## チェックリスト

### アプリ（C#）
- [ ] `MajorVersion` 定数を `"2"` に更新
- [ ] バージョン比較ロジックを追加（v1 ライセンスでの v2 起動時の挙動）
- [ ] インストーラーバージョン更新
- [ ] リリースノート作成

### Stripe
- [ ] v2 商品を作成（メタデータ: `major_version=2`）
- [ ] Payment Link を v2 商品に切り替え
- [ ] テストモードで v2 購入を検証

### Supabase
- [ ] スキーマ変更なし（対応不要）
- [ ] Edge Function 変更なし（対応不要）
- [ ] テスト購入後に `purchased_version = "2"` を確認

### 通知・サポート
- [ ] 既存 v1 ユーザーへの案内メール送信
- [ ] FAQ / ヘルプページにアップグレード方法を記載

---

## バージョンアップ履歴

| バージョン | リリース日 | Stripe 商品 ID | 備考 |
|-----------|-----------|--------------|------|
| v1 | 2026-04 | （Stripe 商品 ID を記入） | 初回リリース |
| v2 | （予定） | （Stripe 商品 ID を記入） | （変更内容を記入） |

---

## 関連ファイル

| ファイル | 内容 |
|---------|------|
| `supabase/functions/stripe-webhook/index.ts` | 購入時の `purchased_version` 記録 |
| `supabase/functions/verify-license/index.ts` | ライセンス検証・バージョン返却 |
| `src/PdfHandler.Core/Models/LicenseActivationsResult.cs` | `PurchasedVersion` プロパティ |
| `src/PdfHandler.Infrastructure/Services/LicenseService.cs` | ライセンス検証結果の処理 |
| `docs/supabase-setup/09_add-purchased-version.sql` | `purchased_version` 列追加マイグレーション |

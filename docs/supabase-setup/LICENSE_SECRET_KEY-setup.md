# LICENSE_SECRET_KEY の設定手順

HMAC ライセンス署名用の秘密鍵を Supabase Edge Functions に設定する手順です。

---

## ⚠️ 重要：「Deploy a new function」では設定できません

**「Deploy a new function」は Edge Function のコードをデプロイする機能です。**  
シークレット（環境変数）の設定場所とは別です。

---

## 正しい設定手順（Supabase ダッシュボード）

### ステップ 1: プロジェクトを開く

1. https://supabase.com/dashboard にアクセス
2. **License Manager** プロジェクトを選択

### ステップ 2: シークレット設定画面へ移動

1. 左サイドバーの **「Project Settings」**（歯車アイコン）をクリック
2. 左メニュー内の **「Edge Functions」** をクリック
3. **「Secrets」** タブまたは **「Manage secrets」** セクションを開く

   - パス例: `Project Settings` → `Edge Functions` → `Secrets`
   - URL 例: `https://supabase.com/dashboard/project/<プロジェクトID>/settings/functions`

### ステップ 3: LICENSE_SECRET_KEY を追加

1. **「Add new secret」** または **「New secret」** をクリック
2. **Name（キー名）**: `LICENSE_SECRET_KEY` と入力
3. **Value（値）**: 下記で生成したランダム文字列を入力
4. **「Save」** をクリック

---

## LICENSE_SECRET_KEY の桁数・形式

### 推奨

| 項目 | 内容 |
|------|------|
| **桁数** | **32文字以上**（推奨: 32〜64文字） |
| **形式** | 英数字（a-z, A-Z, 0-9）のランダム文字列 |
| **用途** | HMAC-SHA256 の署名用秘密鍵 |

HMAC-SHA256 では、鍵長は最低 32 バイト（256 ビット）程度が推奨されます。  
32文字以上の英数字であれば十分な強度になります。

### 生成例（PowerShell）

```powershell
# 64文字のランダム英数字を生成
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object { [char]$_ })
```

### 生成例（手動）

1. パスワードジェネレータで 32 文字以上の英数字を生成
2. または、UUID を 2 つ連結してハイフン除去:  
   `xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`（32文字）

---

## アプリ（デスクトップ）側の設定（オプション）

オフライン HMAC 検証を行うには、**PDF Handler アプリ**にも同じ `LICENSE_SECRET_KEY` を渡す必要があります。

### 方法 1: 環境変数（推奨）

- 変数名: `LICENSE_SECRET_KEY`
- 値: Supabase に設定したものと同じ文字列

### 方法 2: 未設定時

- `LICENSE_SECRET_KEY` が未設定の場合、オフライン検証は行われません
- オンライン時のみライセンス検証が有効になります

---

## 設定後の確認

1. **stripe-webhook** を再デプロイ（シークレット変更後は自動反映される場合あり）
2. テスト購入でライセンスキーが HMAC 付きで発行されることを確認
3. アプリでライセンスキーを入力し、アクティベーションできることを確認

---

## セキュリティ注意

- `LICENSE_SECRET_KEY` を **Git にコミットしない**
- **他人に共有しない**
- 本番用と開発用で **別の値** を使用することを推奨

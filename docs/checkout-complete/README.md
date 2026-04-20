# Stripe 決済完了後の表示ページ（GitHub Pages 用）

`create-checkout-session` は `APP_URL` を基準に次へリダイレクトします。

- 成功: `{APP_URL}/success?session_id={CHECKOUT_SESSION_ID}`
- キャンセル: `{APP_URL}/cancel`

このフォルダは **GitHub Pages の公開ルートがリポジトリの `docs/`** のとき、次の URL で静的ページが配信されます。

- `success/` → `…/checkout-complete/success/`
- `cancel/` → `…/checkout-complete/cancel/`

## 設定手順（例）

1. GitHub リポジトリ → **Settings** → **Pages**
2. **Build and deployment** → Source: **Deploy from a branch**
3. Branch: 通常は **main**、フォルダ **/docs**（プロジェクトに合わせて選択）
4. 保存後、サイト URL は `https://<ユーザー>.github.io/<リポジトリ名>/` の形式になります（Organization やカスタムドメインの場合は異なります）。

5. Supabase Edge Functions の Secrets で **`APP_URL`** を次のように設定します（末尾スラッシュなし）。

   ```text
   https://<ユーザー>.github.io/<リポジトリ名>/checkout-complete
   ```

これで Stripe は `…/checkout-complete/success?session_id=…` にリダイレクトします。  
GitHub Pages は `success` ディレクトリ内の **`index.html`** を `/success/` または `/success` で返します。

ローカル開発では `http://localhost:3000` のように静止サーバを立て、`/docs` と同じパス構造で配信するか、当面エラー回避のみなら `APP_URL` を未設定のまま（関数側デフォルトの localhost）でも構いません。

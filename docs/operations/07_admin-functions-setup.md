# バックオフィス Edge Functions セットアップ手順

> 対象関数: `admin-generate-license` / `admin-list-licenses`  
> GUI ツール: `D:\Users\admin_mak\backoffice\License generator\main.py`

---

## セキュリティ設計の概要

```
【PC の config.json】          【Supabase Secrets（サーバー側）】
  functions_url（公開情報）       LICENSE_SECRET_KEY
  admin_api_key（呼び出し専用）   SUPABASE_SERVICE_ROLE_KEY
                                  RESEND_API_KEY
                                  ADMIN_API_KEY（PC と同じ値）
```

- PC には **ADMIN_API_KEY のみ**。DB への直接アクセス権なし。
- HMAC キー生成・DB INSERT・メール送信はすべて **サーバー側（Edge Function）** で処理。
- `ADMIN_API_KEY` が漏洩しても、できることは「ライセンス発行 API を呼ぶ」のみ。

---

## Step 1: ADMIN_API_KEY を生成する

```powershell
# PowerShell でランダムな 40 文字の API キーを生成
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 40 | % {[char]$_})
```

または OpenSSL が使える場合:
```bash
openssl rand -hex 32
```

生成した値を控えておきます（次の Step で使用）。

---

## Step 2: Supabase Secrets に ADMIN_API_KEY を追加

```bash
npx supabase secrets set ADMIN_API_KEY=ここに生成した値 --project-ref yzmjuotvkxcfnsgleyxl
```

既存シークレットの確認:
```bash
npx supabase secrets list --project-ref yzmjuotvkxcfnsgleyxl
```

---

## Step 3: Edge Functions をデプロイ

`--no-verify-jwt` を必ず付ける（関数が独自の ADMIN_API_KEY 認証を使うため）。

```bash
cd D:\Users\admin_mak\project\pdf-handler

npx supabase functions deploy admin-generate-license --no-verify-jwt --project-ref yzmjuotvkxcfnsgleyxl
npx supabase functions deploy admin-list-licenses    --no-verify-jwt --project-ref yzmjuotvkxcfnsgleyxl
```

デプロイ確認:
```bash
npx supabase functions list --project-ref yzmjuotvkxcfnsgleyxl
```

---

## Step 4: GUI ツールの設定

`main.py` を起動し、「設定」タブで以下を入力して「保存」:

| 項目 | 値 |
|------|-----|
| Supabase Functions URL | `https://yzmjuotvkxcfnsgleyxl.supabase.co/functions/v1` |
| ADMIN_API_KEY | Step 1 で生成した値 |

「接続テスト」ボタンで `✅ 接続成功` が表示されれば完了。

---

## Step 5: 動作確認

### curl で直接テスト（オプション）

```bash
curl -X POST https://yzmjuotvkxcfnsgleyxl.supabase.co/functions/v1/admin-generate-license \
  -H "x-admin-api-key: YOUR_ADMIN_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "appId": "PDFH",
    "formCode": "P101",
    "userEmail": "test@example.com",
    "customerCompany": "テスト株式会社",
    "isActive": false
  }'
```

期待レスポンス:
```json
{
  "success": true,
  "licenseId": "uuid-...",
  "licenseKeyDisplay": "PDFH-P101-XXXX-XXXX-...",
  "emailSent": false
}
```

> `isActive: false` でテストすると、DB に登録されるが無効状態になるので安全。  
> テスト後は Supabase Table Editor で該当行を削除してください。

---

## メールの送受信フロー

```
GUI ツール（admin_api_key のみ）
    │
    │ POST /admin-generate-license
    ▼
Edge Function（サーバー側）
    ├── HMAC キー生成（LICENSE_SECRET_KEY 使用）
    ├── licenses テーブルに INSERT
    └── Resend API でメール送信
            ├── To: 購入者のメールアドレス（ライセンスキー記載）
            └── CC: support@office-goplan.com（管理者への控え）
```

---

## ADMIN_API_KEY のローテーション手順

1. 新しいキーを生成（Step 1）
2. `supabase secrets set ADMIN_API_KEY=新しい値`
3. GUI ツールの設定タブで新しいキーを入力・保存
4. 接続テストで確認

---

## 関連ファイル

| ファイル | 内容 |
|---------|------|
| `supabase/functions/admin-generate-license/index.ts` | ライセンス発行 Edge Function |
| `supabase/functions/admin-list-licenses/index.ts` | 発行履歴取得 Edge Function |
| `D:\Users\admin_mak\backoffice\License generator\main.py` | GUI ツール |
| `docs/operations/06_manual-license-issuance.md` | 手動発行の業務手順 |
| `docs/supabase-setup/15_add_manual_payment_fields.sql` | 手動発行対応マイグレーション |

---

*作成: 2026-04-19*

# Edge Functions レート制限設定手順（FIX-05）

> ⚠️ Supabase Dashboard の Settings タブに Rate Limiting の UI は**存在しません**。  
> レート制限はコード内に実装するか、外部サービス（Upstash Redis）を使う必要があります。

---

## 選択肢の比較

| 方式 | 難易度 | 外部サービス | 信頼性 | 推奨度 |
|------|--------|------------|--------|--------|
| **A. Upstash Redis**（Supabase 公式推奨） | 中 | Upstash（無料枠あり） | 高 | ★★★ |
| **B. 簡易メモリカウンタ**（コード内） | 低 | 不要 | 低（コールドスタートでリセット） | △ |
| C. 今は見送り | - | - | - | ライセンス系は低頻度のため許容可 |

**現状の判断:**  
`stripe-webhook` は Stripe からのみ呼ばれ外部公開リスクが低い。  
`verify-license` は起動時検証のため 1 ユーザーあたりの呼び出し頻度も低い。  
**当面は C（見送り）でも実害は小さいです。本格的に対策するなら A を推奨します。**

---

## 方式 A: Upstash Redis による実装（Supabase 公式推奨）

### 1. Upstash アカウント作成・Redis DB 作成

1. [Upstash Console](https://console.upstash.com/) でアカウント作成（無料枠: 10,000 req/day）
2. **Redis** → **Create Database** → リージョン: `ap-northeast-1`（東京）を選択
3. 作成後、`UPSTASH_REDIS_REST_URL` と `UPSTASH_REDIS_REST_TOKEN` をコピー

### 2. Supabase の Secrets に追加

```bash
npx supabase secrets set UPSTASH_REDIS_REST_URL=https://xxxx.upstash.io --project-ref yzmjuotvkxcfnsgleyxl
npx supabase secrets set UPSTASH_REDIS_REST_TOKEN=AXxx... --project-ref yzmjuotvkxcfnsgleyxl
```

### 3. import_map.json に追加

`supabase/functions/import_map.json` に追記:

```json
{
  "imports": {
    "stripe": "https://esm.sh/stripe@14.21.0?target=deno",
    "@supabase/supabase-js": "https://esm.sh/@supabase/supabase-js@2.49.4",
    "@upstash/ratelimit": "https://esm.sh/@upstash/ratelimit@1.2.1",
    "@upstash/redis": "https://esm.sh/@upstash/redis@1.34.3/deno/index.ts"
  }
}
```

### 4. 各 Edge Function の先頭に追加するコード例

`create-checkout-session/index.ts` への追加例:

```typescript
import { Ratelimit } from "@upstash/ratelimit";
import { Redis } from "@upstash/redis";

const ratelimit = new Ratelimit({
  redis: Redis.fromEnv(),
  limiter: Ratelimit.slidingWindow(10, "1 m"), // 10 req / 1分 / IP
});

// serve() 内の先頭に追加:
const ip = req.headers.get("x-forwarded-for") ?? "unknown";
const { success } = await ratelimit.limit(ip);
if (!success) {
  return jsonResponse(429, { error: "Too many requests" });
}
```

### 推奨設定値

| 関数名 | 制限 | 備考 |
|--------|------|------|
| `create-checkout-session` | 10 req/min/IP | 連打・不正注文防止 |
| `verify-license` | 30 req/min/IP | 起動時チェックを考慮 |
| `get-activations` | 20 req/min/IP | UI 操作分のみ |
| `deactivate-device` | 10 req/min/IP | 操作頻度が低い |
| `update-device-display-name` | 10 req/min/IP | 操作頻度が低い |
| `stripe-webhook` | 設定不要 | Stripe から固定 IP で呼ばれる |

---

## 参考

- [Supabase 公式ドキュメント: Rate Limiting Edge Functions](https://supabase.com/docs/guides/functions/examples/rate-limiting)
- [Upstash Console](https://console.upstash.com/)

---

## 対応ステータス

- [ ] Upstash Redis DB 作成済み
- [ ] Supabase Secrets に `UPSTASH_REDIS_REST_URL` / `TOKEN` 追加済み
- [ ] `import_map.json` に Upstash パッケージ追記済み
- [ ] `create-checkout-session` にレート制限コード追加済み
- [ ] `verify-license` にレート制限コード追加済み
- [ ] その他の関数にレート制限コード追加済み（任意）

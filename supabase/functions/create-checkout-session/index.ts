// Supabase Edge Function: create-checkout-session
// Stripe Checkout セッション作成（stripe-node 非使用: fetch + 公式 REST API で Deno / esm の不整合を回避）

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";

interface RequestBody {
  plan?: string;
  appId?: string;
  customerEmail?: string;
  majorVersion?: string;
}

function jsonResponse(status: number, body: Record<string, unknown>): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: {
      "Content-Type": "application/json",
      "Access-Control-Allow-Origin": "*",
    },
  });
}

function checkoutBaseUrls(): { success_url: string; cancel_url: string } {
  const raw = (Deno.env.get("APP_URL") ?? "").trim().replace(/\/+$/, "");
  if (!raw) {
    throw new Error("APP_URL が未設定です（Edge Functions の Secrets を確認）");
  }
  const base = raw;
  let parsed: URL;
  try {
    parsed = new URL(base);
  } catch {
    throw new Error(`APP_URL が URL として無効です: "${raw}"`);
  }
  if (parsed.protocol !== "http:" && parsed.protocol !== "https:") {
    throw new Error(`APP_URL は http(s) である必要があります: "${base}"`);
  }
  return {
    success_url: `${base}/success?session_id={CHECKOUT_SESSION_ID}`,
    cancel_url: `${base}/cancel`,
  };
}

function isValidEmail(s: string): boolean {
  const t = s.trim();
  if (t.length < 3 || t.length > 254) return false;
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(t);
}

/** C# / JSON のキー揺れを吸収 */
function readCustomerEmail(payload: Record<string, unknown>): string {
  const raw = payload["customerEmail"] ?? payload["CustomerEmail"];
  return typeof raw === "string" ? raw.trim() : "";
}

/** 指定メールの Customer を作成して ID を返す
 *  （customer_balance = 銀行振込を使うには Checkout 前に customer の事前作成が必須） */
async function createCustomer(apiKey: string, email: string): Promise<string> {
  const body = new URLSearchParams();
  body.set("email", email);

  const res = await fetch("https://api.stripe.com/v1/customers", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${apiKey}`,
      "Content-Type": "application/x-www-form-urlencoded",
    },
    body,
  });
  const data = (await res.json()) as {
    id?: string;
    error?: { message?: string; type?: string };
  };
  if (!res.ok || !data.id) {
    const msg = data?.error?.message ?? `HTTP ${res.status}`;
    const typ = data?.error?.type ?? "stripe_error";
    throw new Error(`customer_create: ${typ}: ${msg}`);
  }
  return data.id;
}

/** https://docs.stripe.com/api/checkout/sessions/create */
async function createCheckoutSession(params: {
  apiKey: string;
  priceId: string;
  success_url: string;
  cancel_url: string;
  customerId: string;
  enableJpBankTransfer: boolean;
  metadata: { plan: string; app_id: string; major_version: string };
}): Promise<{ url: string | null }> {
  const body = new URLSearchParams();
  body.set("mode", "payment");
  body.set("success_url", params.success_url);
  body.set("cancel_url", params.cancel_url);
  body.set("customer", params.customerId);
  body.set("locale", "ja");
  body.set("line_items[0][price]", params.priceId);
  body.set("line_items[0][quantity]", "1");
  body.set("metadata[plan]",          params.metadata.plan);
  body.set("metadata[app_id]",        params.metadata.app_id);
  body.set("metadata[major_version]", params.metadata.major_version);

  // 支払方法：既定はカードのみ。銀行振込は Stripe 側で有効化済みの場合だけ追加する。
  body.set("payment_method_types[0]", "card");
  if (params.enableJpBankTransfer) {
    body.set("payment_method_types[1]", "customer_balance");
    body.set("payment_method_options[customer_balance][funding_type]", "bank_transfer");
    body.set("payment_method_options[customer_balance][bank_transfer][type]", "jp_bank_transfer");
  }

  // 請求書（Invoice）の自動発行を有効化
  // → Invoice-XXX.pdf と Receipt-XXX.pdf が正式なフォーマットで発行される
  body.set("invoice_creation[enabled]", "true");
  body.set("invoice_creation[invoice_data][metadata][app_id]",        params.metadata.app_id);
  body.set("invoice_creation[invoice_data][metadata][major_version]", params.metadata.major_version);

  const res = await fetch("https://api.stripe.com/v1/checkout/sessions", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${params.apiKey}`,
      "Content-Type": "application/x-www-form-urlencoded",
    },
    body,
  });

  const data = (await res.json()) as {
    url?: string | null;
    error?: { message?: string; type?: string };
  };

  if (!res.ok) {
    const msg = data?.error?.message ?? `HTTP ${res.status}`;
    const typ = data?.error?.type ?? "stripe_error";
    throw new Error(`${typ}: ${msg}`);
  }

  return { url: data.url ?? null };
}

serve(async (req) => {
  try {
    if (req.method === "OPTIONS") {
      return new Response(null, {
        headers: {
          "Access-Control-Allow-Origin": "*",
          "Access-Control-Allow-Methods": "POST, OPTIONS",
          "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
        },
      });
    }

    let payload: RequestBody;
    try {
      payload = await req.json();
    } catch {
      return jsonResponse(400, { error: "JSON ボディが不正です" });
    }

    const plan = typeof payload?.plan === "string" ? payload.plan.trim() : "";
    const app_id = (typeof payload?.appId === "string" ? payload.appId.trim() : "") || "PDFH";

    if (plan !== "StandardPurchased") {
      return jsonResponse(400, { error: "無効なプランです" });
    }

    const customerEmail = readCustomerEmail(payload as Record<string, unknown>);
    if (!customerEmail || !isValidEmail(customerEmail)) {
      return jsonResponse(400, { error: "customerEmail が無効です" });
    }

    // 本番デプロイ確認用（全文はログに残さない）
    const at = customerEmail.indexOf("@");
    const emailMask = at > 0
      ? `${customerEmail.slice(0, 1)}***${customerEmail.slice(at)}`
      : "***";
    console.log(
      `create-checkout-session: plan=${plan} app_id=${app_id} customerEmail_mask=${emailMask}`,
    );

    const apiKey = (Deno.env.get("STRIPE_SECRET_KEY") ?? "").trim();
    if (!apiKey) {
      return jsonResponse(500, { error: "STRIPE_SECRET_KEY が未設定です（Edge Functions の Secrets を確認）" });
    }

    // アプリ別 Price ID を優先し、未設定なら共通 ID にフォールバック
    // 例: STRIPE_PRICE_ID_PDFH_PURCHASED / STRIPE_PRICE_ID_ZIPS_PURCHASED / STRIPE_PRICE_ID_PICT_PURCHASED
    const priceEnvKey = `STRIPE_PRICE_ID_${app_id}_PURCHASED`;
    const priceId = (Deno.env.get(priceEnvKey) ?? Deno.env.get("STRIPE_PRICE_ID_PURCHASED") ?? "").trim();
    if (!priceId) {
      return jsonResponse(500, {
        error: "買い切り用の Price ID が設定されていません",
      });
    }

    const { success_url, cancel_url } = checkoutBaseUrls();

    // バージョン：リクエスト body → Stripe 商品メタデータ相当の環境変数 の順で取得
    const major_version = (
      typeof (payload as any)?.majorVersion === "string"
        ? (payload as any).majorVersion.trim()
        : ""
    ) || (Deno.env.get("LICENSE_PURCHASED_MAJOR_VERSION") ?? "1");

    const enableJpBankTransfer = (Deno.env.get("STRIPE_ENABLE_JP_BANK_TRANSFER") ?? "")
      .trim()
      .toLowerCase() === "true";

    // Customer はカード決済でも再利用・領収書管理に使えるため作成する。
    // 銀行振込(customer_balance)を有効化する場合も Checkout 前の customer 指定が必要。
    const customerId = await createCustomer(apiKey, customerEmail);

    const session = await createCheckoutSession({
      apiKey,
      priceId,
      success_url,
      cancel_url,
      customerId,
      enableJpBankTransfer,
      metadata: { plan, app_id, major_version },
    });

    if (!session.url) {
      return jsonResponse(500, { error: "Stripe が checkout url を返しませんでした" });
    }

    return jsonResponse(200, { checkoutUrl: session.url });
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    console.error("Error creating checkout session:", message, error);
    // デバッグ時のみ Stripe のエラー詳細をレスポンスに含める
    // （本番安定後は "Internal server error" に戻す）
    return jsonResponse(500, {
      error: "Internal server error",
      detail: message,
    });
  }
});

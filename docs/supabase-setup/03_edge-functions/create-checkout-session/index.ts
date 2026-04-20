// Supabase Edge Function: create-checkout-session
// Stripe Checkout セッション作成（stripe-node 非使用: fetch + 公式 REST API で Deno / esm の不整合を回避）

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";

interface RequestBody {
  plan?: string;
  appId?: string;
  customerEmail?: string;
}

function jsonResponse(status: number, body: Record<string, unknown>): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: {
      "Content-Type": "application/json",
      "Access-Control-Allow-Origin": "*",
      "X-Checkout-Engine": "stripe-rest-v1",
    },
  });
}

function checkoutBaseUrls(): { success_url: string; cancel_url: string } {
  const raw = (Deno.env.get("APP_URL") ?? "").trim().replace(/\/+$/, "");
  const base = raw || "http://localhost:3000";
  let parsed: URL;
  try {
    parsed = new URL(base);
  } catch {
    throw new Error(`APP_URL が URL として無効です: "${base}"`);
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

/** https://docs.stripe.com/api/checkout/sessions/create */
async function createCheckoutSession(params: {
  apiKey: string;
  priceId: string;
  success_url: string;
  cancel_url: string;
  customer_email: string;
  metadata: { plan: string; app_id: string };
}): Promise<{ url: string | null }> {
  const body = new URLSearchParams();
  body.set("mode", "payment");
  body.set("success_url", params.success_url);
  body.set("cancel_url", params.cancel_url);
  body.set("customer_creation", "always");
  body.set("customer_email", params.customer_email);
  body.set("locale", "ja");
  body.set("line_items[0][price]", params.priceId);
  body.set("line_items[0][quantity]", "1");
  body.set("metadata[plan]", params.metadata.plan);
  body.set("metadata[app_id]", params.metadata.app_id);

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

    const priceId = (Deno.env.get("STRIPE_PRICE_ID_PURCHASED") ?? "").trim();
    if (!priceId) {
      return jsonResponse(500, {
        error: "買い切り用の Price ID が設定されていません (STRIPE_PRICE_ID_PURCHASED)",
      });
    }

    const { success_url, cancel_url } = checkoutBaseUrls();

    const session = await createCheckoutSession({
      apiKey,
      priceId,
      success_url,
      cancel_url,
      customer_email: customerEmail,
      metadata: { plan, app_id },
    });

    if (!session.url) {
      return jsonResponse(500, { error: "Stripe が checkout url を返しませんでした" });
    }

    return jsonResponse(200, { checkoutUrl: session.url });
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    console.error("Error creating checkout session:", message, error);
    return jsonResponse(500, { error: message });
  }
});

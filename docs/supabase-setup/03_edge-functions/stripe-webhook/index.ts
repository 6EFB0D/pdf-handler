// Supabase Edge Function: stripe-webhook
// Stripe Webhook（買い切り checkout のみ）

import Stripe from "https://esm.sh/stripe@14.21.0?target=deno";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const stripe = new Stripe(Deno.env.get("STRIPE_SECRET_KEY") || "", {
  apiVersion: "2023-10-16",
  httpClient: Stripe.createFetchHttpClient(),
});

const SUPABASE_URL = Deno.env.get("SUPABASE_URL") || "";
const SUPABASE_SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") || "";
const STRIPE_WEBHOOK_SECRET = Deno.env.get("STRIPE_WEBHOOK_SECRET") || "";

Deno.serve(async (req) => {
  try {
    if (req.method === "OPTIONS") {
      return new Response(null, {
        headers: {
          "Access-Control-Allow-Origin": "*",
          "Access-Control-Allow-Methods": "POST, OPTIONS",
          "Access-Control-Allow-Headers": "stripe-signature, content-type",
        },
      });
    }

    const signature = req.headers.get("stripe-signature");
    if (!signature) {
      return new Response(JSON.stringify({ error: "Stripe signature not found" }), {
        status: 400,
        headers: { "Content-Type": "application/json", "Access-Control-Allow-Origin": "*" },
      });
    }

    const body = await req.text();

    let event: Stripe.Event;
    try {
      event = await stripe.webhooks.constructEventAsync(body, signature, STRIPE_WEBHOOK_SECRET);
    } catch (err) {
      console.error("Webhook signature verification failed:", err.message);
      return new Response(
        JSON.stringify({ error: "Invalid signature", details: err.message }),
        { status: 400, headers: { "Content-Type": "application/json", "Access-Control-Allow-Origin": "*" } },
      );
    }

    const supabase = createClient(SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY);

    if (event.type === "checkout.session.completed") {
      const session = event.data.object as Stripe.Checkout.Session;
      await handleCheckoutCompleted(supabase, session);
    } else {
      console.log(`Ignored event type: ${event.type}`);
    }

    return new Response(JSON.stringify({ received: true }), {
      headers: { "Content-Type": "application/json", "Access-Control-Allow-Origin": "*" },
    });
  } catch (error) {
    console.error("Webhook error:", error.message, error.stack);
    return new Response(JSON.stringify({ error: error.message }), {
      status: 400,
      headers: { "Content-Type": "application/json", "Access-Control-Allow-Origin": "*" },
    });
  }
});

/** 買い切り Standard のみ（形式: {app_id}-P10x-{28hex}-{HMAC8}） */
async function generateLicenseKey(appId: string): Promise<string> {
  const prefix = (appId || "PDFH").toUpperCase();
  const typeCode = "P1";
  const majorVer = Deno.env.get("LICENSE_PURCHASED_MAJOR_VERSION") || "1";
  const verCode = majorVer.padStart(2, "0");
  const formCode = `${typeCode}${verCode}`;

  const uuidHex = crypto.randomUUID().replace(/-/g, "").toUpperCase();
  const serial = uuidHex.substring(0, 28);

  const secretKey = Deno.env.get("LICENSE_SECRET_KEY") || "";
  let hmacSuffix = "";
  if (secretKey) {
    const message = `${prefix}:${formCode}:${serial}`;
    const key = await crypto.subtle.importKey(
      "raw",
      new TextEncoder().encode(secretKey),
      { name: "HMAC", hash: "SHA-256" },
      false,
      ["sign"],
    );
    const sig = await crypto.subtle.sign("HMAC", key, new TextEncoder().encode(message));
    const sigHex = Array.from(new Uint8Array(sig))
      .map((b) => b.toString(16).padStart(2, "0"))
      .join("")
      .toUpperCase();
    hmacSuffix = `-${sigHex.substring(0, 8)}`;
  }

  return `${prefix}-${formCode}-${serial}${hmacSuffix}`;
}

async function handleCheckoutCompleted(supabase: any, session: Stripe.Checkout.Session) {
  const plan = session.metadata?.plan || "";
  const appId = (session.metadata?.app_id || "PDFH").toString().trim().toUpperCase() || "PDFH";

  if (plan !== "StandardPurchased") {
    throw new Error(`Unexpected plan for checkout: ${plan}`);
  }

  let stripeCustomerId: string | null = typeof session.customer === "string"
    ? session.customer
    : session.customer?.id ?? null;
  let stripePaymentIntentId: string | null = typeof session.payment_intent === "string"
    ? session.payment_intent
    : session.payment_intent?.id ?? null;

  if (!stripeCustomerId && stripePaymentIntentId) {
    try {
      const paymentIntent = await stripe.paymentIntents.retrieve(stripePaymentIntentId);
      stripeCustomerId = paymentIntent.customer as string | null ?? null;
    } catch (e) {
      console.warn("Could not retrieve customer from payment intent:", e);
    }
  }

  const licenseKey = await generateLicenseKey(appId);
  const majorVer = Deno.env.get("LICENSE_PURCHASED_MAJOR_VERSION") || "1";

  const licenseData: Record<string, unknown> = {
    license_key: licenseKey,
    app_id: appId,
    plan: "purchased",
    user_email: session.customer_email || session.customer_details?.email || "",
    stripe_customer_id: stripeCustomerId,
    stripe_payment_intent_id: stripePaymentIntentId,
    activation_date: new Date().toISOString(),
    is_active: true,
    expiration_date: null,
    purchased_version: majorVer,
  };

  const { data: license, error: licenseError } = await supabase
    .from("licenses")
    .insert(licenseData)
    .select()
    .single();

  if (licenseError) {
    throw new Error(`Failed to create license: ${licenseError.message}`);
  }

  const userEmail = licenseData.user_email as string;
  if (userEmail && Deno.env.get("RESEND_API_KEY")) {
    try {
      await sendLicenseEmail(userEmail, licenseKey);
      console.log(`License email sent to ${userEmail}`);
    } catch (emailErr) {
      console.error("License email send failed:", emailErr);
    }
  } else {
    console.log(`License created: ${licenseKey} for ${userEmail || "(no email)"}`);
  }
}

async function sendLicenseEmail(toEmail: string, licenseKey: string): Promise<void> {
  const apiKey = Deno.env.get("RESEND_API_KEY");
  const from = Deno.env.get("LICENSE_EMAIL_FROM") || "PDFハンドラ <onboarding@resend.dev>";

  const html = `
<!DOCTYPE html>
<html>
<head><meta charset="utf-8"><title>ライセンスキー</title></head>
<body style="font-family: 'Meiryo', sans-serif; line-height: 1.6; color: #333;">
  <h2>PDFハンドラ ライセンスキー</h2>
  <p>ご購入ありがとうございます。</p>
  <p><strong>プラン:</strong> Standard版（買い切り）</p>
  <p><strong>ライセンスキー:</strong></p>
  <p style="font-size: 18px; font-family: monospace; background: #f5f5f5; padding: 12px; border-radius: 4px;">${licenseKey}</p>
  <p>アプリ内で「ヘルプ」→「ライセンス」→「ライセンスキーを入力」から上記キーを入力してください。</p>
  <hr style="margin: 24px 0; border: none; border-top: 1px solid #ddd;">
  <p style="font-size: 12px; color: #666;">このメールは自動送信されています。ご不明な点はサポートまでお問い合わせください。</p>
</body>
</html>`;

  const res = await fetch("https://api.resend.com/emails", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "Authorization": `Bearer ${apiKey}`,
    },
    body: JSON.stringify({
      from,
      to: [toEmail],
      subject: "【PDFハンドラ】ライセンスキー",
      html,
    }),
  });

  if (!res.ok) {
    const errBody = await res.text();
    throw new Error(`Resend API error ${res.status}: ${errBody}`);
  }
}

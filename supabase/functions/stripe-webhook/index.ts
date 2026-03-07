// Supabase Edge Function: stripe-webhook
// Stripe Webhookイベントを処理する
//
// 重要: この関数はStripe Webhookからのリクエストを受け付けるため、
// JWT認証を無効化する必要があります。
//
// deno.json ファイルの deploy.verify_jwt: false 設定により、
// Supabase API GatewayでのJWT検証がスキップされ、
// Stripeからの認証ヘッダーなしリクエストが通過します。
// セキュリティはStripe署名検証（STRIPE_WEBHOOK_SECRET）で確保されます。
//
// デプロイ方法:
//   supabase functions deploy stripe-webhook --project-ref yzmjuotvkxcfnsgleyxl

import Stripe from "https://esm.sh/stripe@14.21.0?target=deno";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const stripe = new Stripe(Deno.env.get("STRIPE_SECRET_KEY") || "", {
  apiVersion: "2023-10-16",
  httpClient: Stripe.createFetchHttpClient(),
});

const SUPABASE_URL = Deno.env.get("SUPABASE_URL") || "";
const SUPABASE_SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") || "";
const STRIPE_WEBHOOK_SECRET = Deno.env.get("STRIPE_WEBHOOK_SECRET") || "";

// Deno.serve を使用（JWT検証は deno.json の deploy.verify_jwt: false で無効化）
Deno.serve(async (req) => {
  try {
    // CORSプリフライトリクエストの処理
    if (req.method === "OPTIONS") {
      return new Response(null, {
        headers: {
          "Access-Control-Allow-Origin": "*",
          "Access-Control-Allow-Methods": "POST, OPTIONS",
          "Access-Control-Allow-Headers": "stripe-signature, content-type",
        },
      });
    }

    console.log("Webhook received:", req.method, req.url);
    console.log("Headers:", JSON.stringify(Object.fromEntries(req.headers.entries())));

    // Stripe署名の検証（これが認証の役割を果たす）
    const signature = req.headers.get("stripe-signature");
    if (!signature) {
      console.error("Stripe signature not found in headers");
      return new Response(
        JSON.stringify({ error: "Stripe signature not found" }),
        { 
          status: 400,
          headers: { 
            "Content-Type": "application/json",
            "Access-Control-Allow-Origin": "*",
          }
        }
      );
    }

    const body = await req.text();
    console.log("Request body length:", body.length);
    
    // Stripe署名を検証（これでリクエストの正当性を確認）
    let event: Stripe.Event;
    try {
      event = await stripe.webhooks.constructEventAsync(
        body,
        signature,
        STRIPE_WEBHOOK_SECRET
      );
      console.log("Webhook signature verified successfully. Event type:", event.type);
    } catch (err) {
      console.error("Webhook signature verification failed:", err.message);
      return new Response(
        JSON.stringify({ error: "Invalid signature", details: err.message }),
        { 
          status: 400,
          headers: { 
            "Content-Type": "application/json",
            "Access-Control-Allow-Origin": "*",
          }
        }
      );
    }

    const supabase = createClient(SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY);

    switch (event.type) {
      case "checkout.session.completed": {
        console.log("Processing checkout.session.completed");
        const session = event.data.object as Stripe.Checkout.Session;
        await handleCheckoutCompleted(supabase, session);
        break;
      }

      case "customer.subscription.updated": {
        console.log("Processing customer.subscription.updated");
        const subscription = event.data.object as Stripe.Subscription;
        await handleSubscriptionUpdated(supabase, subscription);
        break;
      }

      case "customer.subscription.deleted": {
        console.log("Processing customer.subscription.deleted");
        const subscription = event.data.object as Stripe.Subscription;
        await handleSubscriptionDeleted(supabase, subscription);
        break;
      }

      case "invoice.payment_succeeded": {
        console.log("Processing invoice.payment_succeeded");
        const invoice = event.data.object as Stripe.Invoice;
        await handleInvoicePaymentSucceeded(supabase, invoice);
        break;
      }

      default:
        console.log(`Unhandled event type: ${event.type}`);
    }

    console.log("Webhook processed successfully");
    return new Response(JSON.stringify({ received: true }), {
      headers: { 
        "Content-Type": "application/json",
        "Access-Control-Allow-Origin": "*",
      },
    });
  } catch (error) {
    console.error("Webhook error:", error.message, error.stack);
    return new Response(
      JSON.stringify({ error: error.message }),
      { 
        status: 400,
        headers: { 
          "Content-Type": "application/json",
          "Access-Control-Allow-Origin": "*",
        }
      }
    );
  }
});

/**
 * ライセンスキーを生成（形式: PDFH-XXXX-28文字）
 * XXXX: P101(買い切りv1), P102(買い切りv2), S100(サブStandard), S200(サブPremium)
 */
function generateLicenseKey(plan: string, isSubscription: boolean): string {
  let typeCode: string;
  let verCode: string;
  if (isSubscription) {
    typeCode = plan === "Premium" ? "S2" : "S1";
    verCode = "00"; // サブスクは全バージョン
  } else {
    typeCode = "P1"; // 買い切りは StandardPurchased のみ
    const majorVer = Deno.env.get("LICENSE_PURCHASED_MAJOR_VERSION") || "1";
    verCode = majorVer.padStart(2, "0"); // 01, 02, ...
  }
  const formCode = `${typeCode}${verCode}`; // P101, S100, S200

  // 28文字のシリアル（16進数大文字）
  const uuidHex = crypto.randomUUID().replace(/-/g, "").toUpperCase();
  const serial = uuidHex.substring(0, 28);

  return `PDFH-${formCode}-${serial}`;
}

async function handleCheckoutCompleted(
  supabase: any,
  session: Stripe.Checkout.Session
) {
  const plan = session.metadata?.plan || "";
  const isSubscription = session.metadata?.isSubscription === "true";

  // セッションを展開してcustomer_idを確実に取得（session.customerがnullの場合に対応）
  let stripeCustomerId: string | null = typeof session.customer === "string"
    ? session.customer
    : session.customer?.id ?? null;
  let stripePaymentIntentId: string | null = typeof session.payment_intent === "string"
    ? session.payment_intent
    : session.payment_intent?.id ?? null;

  // customer_idがnullの場合はpayment_intentから取得
  if (!stripeCustomerId && stripePaymentIntentId) {
    try {
      const paymentIntent = await stripe.paymentIntents.retrieve(stripePaymentIntentId);
      stripeCustomerId = paymentIntent.customer as string | null ?? null;
    } catch (e) {
      console.warn("Could not retrieve customer from payment intent:", e);
    }
  }

  // プランをデータベース形式に変換
  let dbPlan: string;
  if (plan === "StandardPurchased") {
    dbPlan = "purchased";
  } else if (plan === "StandardSubscription") {
    dbPlan = "subscription_standard";
  } else if (plan === "Premium") {
    dbPlan = "subscription_premium";
  } else {
    throw new Error(`Invalid plan: ${plan}`);
  }

  // ライセンスキーを生成（形式: PDFH-XXXX-28文字）
  const licenseKey = generateLicenseKey(plan, isSubscription);

  // ライセンスを作成
  const licenseData: any = {
    license_key: licenseKey,
    plan: dbPlan,
    user_email: session.customer_email || session.customer_details?.email || "",
    stripe_customer_id: stripeCustomerId,
    stripe_payment_intent_id: stripePaymentIntentId,
    activation_date: new Date().toISOString(),
    is_active: true,
  };

  if (isSubscription) {
    licenseData.stripe_subscription_id = session.subscription as string;
    // サブスクリプションの更新日を設定（1年後）
    const renewalDate = new Date();
    renewalDate.setFullYear(renewalDate.getFullYear() + 1);
    licenseData.subscription_renewal_date = renewalDate.toISOString();
    // サブスクは全バージョン対応のため purchased_version は null
  } else {
    // 買い切り版は有効期限なし、purchased_version を形態コードから設定
    licenseData.expiration_date = null;
    const majorVer = Deno.env.get("LICENSE_PURCHASED_MAJOR_VERSION") || "1";
    licenseData.purchased_version = majorVer;
  }

  const { data: license, error: licenseError } = await supabase
    .from("licenses")
    .insert(licenseData)
    .select()
    .single();

  if (licenseError) {
    throw new Error(`Failed to create license: ${licenseError.message}`);
  }

  // サブスクリプションの場合はsubscriptionsテーブルにも追加
  if (isSubscription && session.subscription) {
    const subscription = await stripe.subscriptions.retrieve(
      session.subscription as string
    );

    await supabase.from("subscriptions").insert({
      license_id: license.id,
      stripe_subscription_id: subscription.id,
      status: subscription.status === "active" ? "active" : "canceled",
      current_period_start: new Date(subscription.current_period_start * 1000).toISOString(),
      current_period_end: new Date(subscription.current_period_end * 1000).toISOString(),
      cancel_at_period_end: subscription.cancel_at_period_end || false,
    });
  }

  // ライセンスキーをメールで送信（RESEND_API_KEY が設定されている場合）
  const userEmail = licenseData.user_email;
  if (userEmail && Deno.env.get("RESEND_API_KEY")) {
    try {
      await sendLicenseEmail(userEmail, licenseKey, dbPlan, isSubscription);
      console.log(`License email sent to ${userEmail}`);
    } catch (emailErr) {
      console.error("License email send failed:", emailErr);
      // メール送信失敗でもライセンス作成は成功しているため、処理は継続
    }
  } else {
    console.log(`License created: ${licenseKey} for ${userEmail || "(no email)"}`);
  }
}

/**
 * Resend API でライセンスキーをメール送信
 * 環境変数: RESEND_API_KEY, LICENSE_EMAIL_FROM（例: PDFハンドラ <noreply@yourdomain.com>）
 */
async function sendLicenseEmail(
  toEmail: string,
  licenseKey: string,
  plan: string,
  isSubscription: boolean
): Promise<void> {
  const apiKey = Deno.env.get("RESEND_API_KEY");
  const from = Deno.env.get("LICENSE_EMAIL_FROM") || "PDFハンドラ <onboarding@resend.dev>";

  const planLabel = plan === "purchased"
    ? "Standard版（買い切り）"
    : plan === "subscription_standard"
    ? "Standard版（サブスクリプション）"
    : "Premium版（サブスクリプション）";

  const html = `
<!DOCTYPE html>
<html>
<head><meta charset="utf-8"><title>ライセンスキー</title></head>
<body style="font-family: 'Meiryo', sans-serif; line-height: 1.6; color: #333;">
  <h2>PDFハンドラ ライセンスキー</h2>
  <p>ご購入ありがとうございます。</p>
  <p><strong>プラン:</strong> ${planLabel}</p>
  <p><strong>ライセンスキー:</strong></p>
  <p style="font-size: 18px; font-family: monospace; background: #f5f5f5; padding: 12px; border-radius: 4px;">${licenseKey}</p>
  <p>アプリ内で「ヘルプ」→「ライセンス」→「ライセンスキーを入力」から上記キーを入力してください。</p>
  ${isSubscription ? "<p>サブスクリプションは年額更新です。解約はStripeのカスタマーポータルからいつでも可能です。</p>" : ""}
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

async function handleSubscriptionUpdated(
  supabase: any,
  subscription: Stripe.Subscription
) {
  const { data: license } = await supabase
    .from("licenses")
    .select("id")
    .eq("stripe_subscription_id", subscription.id)
    .single();

  if (!license) return;

  // subscriptionsテーブルを更新
  await supabase
    .from("subscriptions")
    .update({
      status: subscription.status === "active" ? "active" : "canceled",
      current_period_start: new Date(subscription.current_period_start * 1000).toISOString(),
      current_period_end: new Date(subscription.current_period_end * 1000).toISOString(),
      cancel_at_period_end: subscription.cancel_at_period_end || false,
    })
    .eq("stripe_subscription_id", subscription.id);

  // ライセンスの更新日を更新
  if (subscription.status === "active") {
    const renewalDate = new Date(subscription.current_period_end * 1000);
    await supabase
      .from("licenses")
      .update({
        subscription_renewal_date: renewalDate.toISOString(),
      })
      .eq("id", license.id);
  }
}

async function handleSubscriptionDeleted(
  supabase: any,
  subscription: Stripe.Subscription
) {
  const { data: license } = await supabase
    .from("licenses")
    .select("id")
    .eq("stripe_subscription_id", subscription.id)
    .single();

  if (!license) return;

  // subscriptionsテーブルを更新
  await supabase
    .from("subscriptions")
    .update({
      status: "canceled",
    })
    .eq("stripe_subscription_id", subscription.id);

  // ライセンスを無効化
  await supabase
    .from("licenses")
    .update({
      is_active: false,
    })
    .eq("id", license.id);
}

async function handleInvoicePaymentSucceeded(
  supabase: any,
  invoice: Stripe.Invoice
) {
  if (!invoice.subscription) return;

  const subscription = await stripe.subscriptions.retrieve(
    invoice.subscription as string
  );

  await handleSubscriptionUpdated(supabase, subscription);
}


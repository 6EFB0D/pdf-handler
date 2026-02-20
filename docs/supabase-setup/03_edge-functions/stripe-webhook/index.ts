// Supabase Edge Function: stripe-webhook
// Stripe Webhookイベントを処理する

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import Stripe from "https://esm.sh/stripe@14.21.0?target=deno";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const stripe = new Stripe(Deno.env.get("STRIPE_SECRET_KEY") || "", {
  apiVersion: "2023-10-16",
  httpClient: Stripe.createFetchHttpClient(),
});

const SUPABASE_URL = Deno.env.get("SUPABASE_URL") || "";
const SUPABASE_SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") || "";
const STRIPE_WEBHOOK_SECRET = Deno.env.get("STRIPE_WEBHOOK_SECRET") || "";

// serve関数で認証をスキップ（Stripe署名検証でセキュリティを確保）
serve(async (req) => {
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

    // Stripe署名の検証（これが認証の役割を果たす）
    const signature = req.headers.get("stripe-signature");
    if (!signature) {
      console.error("Stripe signature not found");
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
    
    // Stripe署名を検証（これでリクエストの正当性を確認）
    let event: Stripe.Event;
    try {
      event = stripe.webhooks.constructEvent(
        body,
        signature,
        STRIPE_WEBHOOK_SECRET
      );
    } catch (err) {
      console.error("Webhook signature verification failed:", err);
      return new Response(
        JSON.stringify({ error: "Invalid signature" }),
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
        const session = event.data.object as Stripe.Checkout.Session;
        await handleCheckoutCompleted(supabase, session);
        break;
      }

      case "customer.subscription.updated": {
        const subscription = event.data.object as Stripe.Subscription;
        await handleSubscriptionUpdated(supabase, subscription);
        break;
      }

      case "customer.subscription.deleted": {
        const subscription = event.data.object as Stripe.Subscription;
        await handleSubscriptionDeleted(supabase, subscription);
        break;
      }

      case "invoice.payment_succeeded": {
        const invoice = event.data.object as Stripe.Invoice;
        await handleInvoicePaymentSucceeded(supabase, invoice);
        break;
      }

      default:
        console.log(`Unhandled event type: ${event.type}`);
    }

    return new Response(JSON.stringify({ received: true }), {
      headers: { 
        "Content-Type": "application/json",
        "Access-Control-Allow-Origin": "*",
      },
    });
  } catch (error) {
    console.error("Webhook error:", error);
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
}, { auth: false });

async function handleCheckoutCompleted(
  supabase: any,
  session: Stripe.Checkout.Session
) {
  const plan = session.metadata?.plan || "";
  const isSubscription = session.metadata?.isSubscription === "true";

  // ライセンスキーを生成
  const licenseKey = `PDFH-${crypto.randomUUID().toUpperCase().replace(/-/g, "")}`;

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

  // ライセンスを作成
  const licenseData: any = {
    license_key: licenseKey,
    plan: dbPlan,
    user_email: session.customer_email || session.customer_details?.email || "",
    stripe_customer_id: session.customer as string,
    stripe_payment_intent_id: session.payment_intent as string,
    activation_date: new Date().toISOString(),
    is_active: true,
  };

  if (isSubscription) {
    licenseData.stripe_subscription_id = session.subscription as string;
    // サブスクリプションの更新日を設定（1年後）
    const renewalDate = new Date();
    renewalDate.setFullYear(renewalDate.getFullYear() + 1);
    licenseData.subscription_renewal_date = renewalDate.toISOString();
  } else {
    // 買い切り版は有効期限なし
    licenseData.expiration_date = null;
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

  // TODO: ライセンスキーをメールで送信（SendGrid、Resend等を使用）
  console.log(`License created: ${licenseKey} for ${licenseData.user_email}`);
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




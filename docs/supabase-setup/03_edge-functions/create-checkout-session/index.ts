// Supabase Edge Function: create-checkout-session
// Stripe Checkoutセッションを作成する

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import Stripe from "https://esm.sh/stripe@14.21.0?target=deno";

const stripe = new Stripe(Deno.env.get("STRIPE_SECRET_KEY") || "", {
  apiVersion: "2023-10-16",
  httpClient: Stripe.createFetchHttpClient(),
});

const SUPABASE_URL = Deno.env.get("SUPABASE_URL") || "";
const SUPABASE_SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") || "";

interface RequestBody {
  plan: string;
  isSubscription: boolean;
}

serve(async (req) => {
  try {
    // CORSヘッダー
    if (req.method === "OPTIONS") {
      return new Response(null, {
        headers: {
          "Access-Control-Allow-Origin": "*",
          "Access-Control-Allow-Methods": "POST, OPTIONS",
          "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
        },
      });
    }

    const { plan, isSubscription }: RequestBody = await req.json();

    // Premium版が非公開の場合は拒否
    if (plan === "Premium" && Deno.env.get("ENABLE_PREMIUM_PLAN") !== "true") {
      return new Response(
        JSON.stringify({ error: "Premiumプランは現在公開されていません" }),
        {
          status: 400,
          headers: {
            "Content-Type": "application/json",
            "Access-Control-Allow-Origin": "*",
          },
        }
      );
    }

    // Stripe Price IDのマッピング（Stripeダッシュボードで作成したPrice IDに置き換えてください）
    const priceIds: Record<string, string> = {
      "StandardPurchased": Deno.env.get("STRIPE_PRICE_ID_PURCHASED") || "",
      "StandardSubscription": Deno.env.get("STRIPE_PRICE_ID_SUBSCRIPTION_STANDARD") || "",
      "Premium": Deno.env.get("STRIPE_PRICE_ID_SUBSCRIPTION_PREMIUM") || "",
    };

    const priceId = priceIds[plan];
    if (!priceId) {
      return new Response(
        JSON.stringify({ error: "無効なプランです" }),
        {
          status: 400,
          headers: {
            "Content-Type": "application/json",
            "Access-Control-Allow-Origin": "*",
          },
        }
      );
    }

    // Stripe Checkoutセッションを作成
    const sessionParams: Stripe.Checkout.SessionCreateParams = {
      mode: isSubscription ? "subscription" : "payment",
      line_items: [
        {
          price: priceId,
          quantity: 1,
        },
      ],
      success_url: `${Deno.env.get("APP_URL") || "http://localhost:3000"}/success?session_id={CHECKOUT_SESSION_ID}`,
      cancel_url: `${Deno.env.get("APP_URL") || "http://localhost:3000"}/cancel`,
      metadata: {
        plan: plan,
        isSubscription: isSubscription.toString(),
      },
    };

    const session = await stripe.checkout.sessions.create(sessionParams);

    return new Response(
      JSON.stringify({ checkoutUrl: session.url }),
      {
        headers: {
          "Content-Type": "application/json",
          "Access-Control-Allow-Origin": "*",
        },
      }
    );
  } catch (error) {
    console.error("Error creating checkout session:", error);
    return new Response(
      JSON.stringify({ error: error.message }),
      {
        status: 500,
        headers: {
          "Content-Type": "application/json",
          "Access-Control-Allow-Origin": "*",
        },
      }
    );
  }
});




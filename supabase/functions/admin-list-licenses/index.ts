// Supabase Edge Function: admin-list-licenses
// バックオフィス用・ライセンス一覧取得（Stripe・手動両対応）
//
// 認証: x-admin-api-key ヘッダー
// クエリパラメータ:
//   limit        件数（デフォルト 40、最大 100）
//   search       メールアドレス・法人名の部分一致
//   paymentType  stripe | manual | （未指定=全件）
//   activeOnly   true のときアクティブのみ

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "@supabase/supabase-js";

const SUPABASE_URL              = Deno.env.get("SUPABASE_URL") ?? "";
const SUPABASE_SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";
const ADMIN_API_KEY             = Deno.env.get("ADMIN_API_KEY") ?? "";
const JSON_HEADERS = { "Content-Type": "application/json" };

serve(async (req) => {
  if (req.method !== "GET") {
    return new Response(JSON.stringify({ error: "Method not allowed" }), { status: 405, headers: JSON_HEADERS });
  }

  const incoming = req.headers.get("x-admin-api-key") ?? "";
  if (!ADMIN_API_KEY || incoming !== ADMIN_API_KEY) {
    return new Response(JSON.stringify({ error: "Unauthorized" }), { status: 401, headers: JSON_HEADERS });
  }

  try {
    const url         = new URL(req.url);
    const limit       = Math.min(parseInt(url.searchParams.get("limit") ?? "40"), 100);
    const search      = url.searchParams.get("search")?.trim() ?? "";
    const paymentType = url.searchParams.get("paymentType")?.trim() ?? "";
    const activeOnly  = url.searchParams.get("activeOnly") === "true";

    const supabase = createClient(SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY);

    let query = supabase
      .from("licenses")
      .select(
        "id, app_id, user_email, customer_company, customer_contact, " +
        "invoice_number, is_active, payment_type, payment_confirmed_at, " +
        "stripe_payment_intent_id, stripe_checkout_session_id, " +
        "purchased_version, notes, created_at",
      )
      .order("created_at", { ascending: false })
      .limit(limit);

    if (paymentType === "stripe" || paymentType === "manual") {
      query = query.eq("payment_type", paymentType);
    }
    if (activeOnly) {
      query = query.eq("is_active", true);
    }
    if (search) {
      query = query.or(`user_email.ilike.%${search}%,customer_company.ilike.%${search}%`);
    }

    const { data, error } = await query;

    if (error) {
      console.error("admin-list-licenses query error:", error);
      return new Response(JSON.stringify({ error: "Internal server error" }), { status: 500, headers: JSON_HEADERS });
    }

    return new Response(
      JSON.stringify({ licenses: data ?? [], count: (data ?? []).length }),
      { status: 200, headers: JSON_HEADERS },
    );
  } catch (err) {
    console.error("admin-list-licenses:", err);
    return new Response(JSON.stringify({ error: "Internal server error" }), { status: 500, headers: JSON_HEADERS });
  }
});

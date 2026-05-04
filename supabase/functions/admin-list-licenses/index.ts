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

/** licenses に transaction_id がある前提の SELECT（マイグレ 18 未適用の PROD では失敗しうる） */
const SELECT_WITH_TXN =
  "id, license_key, app_id, user_email, customer_company, customer_contact, " +
  "transaction_id, invoice_number, is_active, activation_count, " +
  "revoked_at, revoked_reason, payment_type, payment_confirmed_at, " +
  "stripe_payment_intent_id, stripe_checkout_session_id, " +
  "purchased_version, notes, created_at";

const SELECT_WITHOUT_TXN =
  "id, license_key, app_id, user_email, customer_company, customer_contact, " +
  "invoice_number, is_active, activation_count, " +
  "revoked_at, revoked_reason, payment_type, payment_confirmed_at, " +
  "stripe_payment_intent_id, stripe_checkout_session_id, " +
  "purchased_version, notes, created_at";

function isMissingTransactionIdColumn(err: {
  message?: string | null;
  details?: string | null;
  hint?: string | null;
}): boolean {
  const blob = [err.message, err.details, err.hint]
    .filter(Boolean)
    .join(" ")
    .toLowerCase();
  return blob.includes("transaction_id");
}

function jsonError(status: number, message: string, details?: string) {
  const body: Record<string, string> = { error: message };
  if (details) body.details = details;
  return new Response(JSON.stringify(body), { status, headers: JSON_HEADERS });
}

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

    const buildQuery = (selectList: string, includeTxnInSearch: boolean) => {
      let q = supabase
        .from("licenses")
        .select(selectList)
        .order("created_at", { ascending: false })
        .limit(limit);

      if (paymentType === "stripe" || paymentType === "manual") {
        q = q.eq("payment_type", paymentType);
      }
      if (activeOnly) {
        q = q.eq("is_active", true);
      }
      if (search) {
        const orExpr = includeTxnInSearch
          ? `user_email.ilike.%${search}%,customer_company.ilike.%${search}%,transaction_id.ilike.%${search}%,invoice_number.ilike.%${search}%,license_key.ilike.%${search}%`
          : `user_email.ilike.%${search}%,customer_company.ilike.%${search}%,invoice_number.ilike.%${search}%,license_key.ilike.%${search}%`;
        q = q.or(orExpr);
      }
      return q;
    };

    let { data, error } = await buildQuery(SELECT_WITH_TXN, true);

    if (error && isMissingTransactionIdColumn(error)) {
      console.warn(
        "admin-list-licenses: transaction_id 列なし — フォールバック SELECT（18_add_transaction_id を PROD で実行してください）",
      );
      const second = await buildQuery(SELECT_WITHOUT_TXN, false);
      data = second.data;
      error = second.error;
    }

    if (error) {
      console.error("admin-list-licenses query error:", error);
      return jsonError(
        500,
        "Internal server error",
        [error.message, error.code, error.details].filter(Boolean).join(" — "),
      );
    }

    const rows = (data ?? []).map((row: Record<string, unknown>) =>
      row.transaction_id === undefined ? { ...row, transaction_id: null } : row,
    );
    const licenseIds = rows.map((row) => row.id).filter(Boolean);
    if (licenseIds.length > 0) {
      const { data: activeActivations, error: activationError } = await supabase
        .from("license_activations")
        .select("license_id")
        .in("license_id", licenseIds)
        .eq("is_active", true);

      if (activationError) {
        console.error("admin-list-licenses activation count error:", activationError);
        return jsonError(
          500,
          "Internal server error",
          [
            activationError.message,
            activationError.code,
            activationError.details,
          ].filter(Boolean).join(" — "),
        );
      }

      const counts = new Map<string, number>();
      for (const activation of activeActivations ?? []) {
        const licenseId = activation.license_id;
        counts.set(licenseId, (counts.get(licenseId) ?? 0) + 1);
      }
      for (const row of rows) {
        row.activation_count = counts.get(row.id) ?? 0;
      }
    }

    return new Response(
      JSON.stringify({ licenses: rows, count: rows.length }),
      { status: 200, headers: JSON_HEADERS },
    );
  } catch (err) {
    console.error("admin-list-licenses:", err);
    const msg = err instanceof Error ? err.message : String(err);
    return jsonError(500, "Internal server error", msg);
  }
});

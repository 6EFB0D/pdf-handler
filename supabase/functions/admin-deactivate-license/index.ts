// Supabase Edge Function: admin-deactivate-license
// ライセンスの無効化（返金・誤発行対応）
//
// 認証: x-admin-api-key ヘッダー
// デプロイ: supabase functions deploy admin-deactivate-license --no-verify-jwt

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "@supabase/supabase-js";

const SUPABASE_URL              = Deno.env.get("SUPABASE_URL") ?? "";
const SUPABASE_SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";
const ADMIN_API_KEY             = Deno.env.get("ADMIN_API_KEY") ?? "";
const JSON_HEADERS = { "Content-Type": "application/json" };

serve(async (req) => {
  if (req.method !== "POST") {
    return new Response(JSON.stringify({ error: "Method not allowed" }), { status: 405, headers: JSON_HEADERS });
  }

  const incoming = req.headers.get("x-admin-api-key") ?? "";
  if (!ADMIN_API_KEY || incoming !== ADMIN_API_KEY) {
    return new Response(JSON.stringify({ error: "Unauthorized" }), { status: 401, headers: JSON_HEADERS });
  }

  try {
    const { licenseId, licenseKey, reason } = await req.json();

    if (!licenseId && !licenseKey) {
      return new Response(
        JSON.stringify({ error: "licenseId または licenseKey が必要です" }),
        { status: 400, headers: JSON_HEADERS },
      );
    }

    const supabase = createClient(SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY);

    // ライセンス検索
    const query = licenseId
      ? supabase.from("licenses").select("*").eq("id", licenseId)
      : supabase.from("licenses").select("*").eq("license_key", licenseKey);
    const { data: license, error: findError } = await query.maybeSingle();

    if (findError || !license) {
      return new Response(
        JSON.stringify({ error: "ライセンスが見つかりません" }),
        { status: 404, headers: JSON_HEADERS },
      );
    }
    if (!license.is_active) {
      return new Response(
        JSON.stringify({ error: "このライセンスは既に無効化されています", license }),
        { status: 409, headers: JSON_HEADERS },
      );
    }

    // 無効化
    const ts        = new Date().toLocaleString("ja-JP", { timeZone: "Asia/Tokyo" });
    const noteText  = `【無効化 ${ts}】${reason ?? "管理者操作"}`;
    const newNotes  = license.notes ? `${license.notes}\n${noteText}` : noteText;

    const revokedAt = new Date().toISOString();
    const revokedReason = reason ?? "管理者操作";

    const { data: updated, error: updateError } = await supabase
      .from("licenses")
      .update({
        is_active: false,
        revoked_at: revokedAt,
        revoked_reason: revokedReason,
        notes: newNotes,
      })
      .eq("id", license.id)
      .select()
      .single();

    if (updateError) {
      console.error("Update error:", updateError);
      return new Response(JSON.stringify({ error: "Internal server error" }), { status: 500, headers: JSON_HEADERS });
    }

    return new Response(JSON.stringify({ success: true, license: updated }), { status: 200, headers: JSON_HEADERS });

  } catch (err) {
    console.error("admin-deactivate-license:", err);
    return new Response(JSON.stringify({ error: "Internal server error" }), { status: 500, headers: JSON_HEADERS });
  }
});

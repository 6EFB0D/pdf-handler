// Supabase Edge Function: get-activations
// ライセンスに紐づくアクティベーション一覧を取得（ライセンス管理ダイアログ用）

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "@supabase/supabase-js";
import { assertLicenseBelongsToClientApp } from "../_shared/license-app-guard.ts";
import { licenseDbLookupKeys } from "../_shared/license-db-lookup.ts";

const SUPABASE_URL = Deno.env.get("SUPABASE_URL") || "";
const SUPABASE_SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") || "";

interface RequestBody {
  licenseKey: string;
  hardwareId: string; // 呼び出し元のハードウェアID（認証・「このPC」判定に使用）
  /** PDFH / ZIPS / PICT … verify-license と同じ */
  clientAppId: string;
}

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Content-Type": "application/json",
};

serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response(null, { headers: corsHeaders });
  }

  try {
    const { licenseKey, hardwareId, clientAppId }: RequestBody = await req.json();

    if (!licenseKey || !hardwareId) {
      return new Response(
        JSON.stringify({ error: "ライセンスキーとハードウェアIDが必要です" }),
        { status: 400, headers: corsHeaders }
      );
    }

    const supabase = createClient(SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY);

    const lookupKeys = licenseDbLookupKeys(licenseKey);
    const { data: licenseRows, error: lookupErr } = await supabase
      .from("licenses")
      .select("id, app_id, license_key")
      .in("license_key", lookupKeys)
      .eq("is_active", true)
      .limit(5);

    if (lookupErr) {
      console.error("get-activations license lookup:", lookupErr);
      return new Response(
        JSON.stringify({ error: "Internal server error" }),
        { status: 500, headers: corsHeaders }
      );
    }

    const rows = licenseRows ?? [];
    if (rows.length !== 1) {
      return new Response(
        JSON.stringify({ error: "ライセンスが見つかりません" }),
        { status: 404, headers: corsHeaders }
      );
    }

    const license = rows[0];

    const appGuard = assertLicenseBelongsToClientApp(license, clientAppId);
    if (!appGuard.ok) {
      return new Response(
        JSON.stringify({ error: appGuard.userMessage }),
        { status: 403, headers: corsHeaders }
      );
    }

    // 呼び出し元のハードウェアIDがこのライセンスにアクティベートされているか確認
    const { data: callerActivation } = await supabase
      .from("license_activations")
      .select("id")
      .eq("license_id", license.id)
      .eq("hardware_id", hardwareId)
      .eq("is_active", true)
      .single();

    if (!callerActivation) {
      return new Response(
        JSON.stringify({ error: "このデバイスはライセンスにアクティベートされていません" }),
        { status: 403, headers: corsHeaders }
      );
    }

    // アクティベーション一覧を取得（is_active = true のみ）
    const { data: activations, error: activationsError } = await supabase
      .from("license_activations")
      .select("id, hardware_id, device_name, display_name, activation_date, last_verification_date")
      .eq("license_id", license.id)
      .eq("is_active", true)
      .order("activation_date", { ascending: true });

    if (activationsError) {
      console.error("get-activations error:", activationsError);
      return new Response(
        JSON.stringify({ error: "Internal server error" }),
        { status: 500, headers: corsHeaders }
      );
    }

    // 表示名を決定: display_name ?? device_name ?? "デバイスN"
    const result = (activations ?? []).map((a, index) => ({
      id: a.id,
      isCurrentDevice: a.hardware_id === hardwareId,
      displayName: a.display_name ?? a.device_name ?? `デバイス${index + 1}`,
      deviceName: a.device_name,
      activationDate: a.activation_date,
      lastVerificationDate: a.last_verification_date,
    }));

    const DEVICE_LIMIT = 3;

    return new Response(
      JSON.stringify({
        activations: result,
        deviceLimit: DEVICE_LIMIT,
        deviceCount: result.length,
      }),
      { headers: corsHeaders }
    );
  } catch (error) {
    console.error("get-activations:", error);
    return new Response(
      JSON.stringify({ error: "Internal server error" }),
      { status: 500, headers: corsHeaders }
    );
  }
});

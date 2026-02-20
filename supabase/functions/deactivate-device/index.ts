// Supabase Edge Function: deactivate-device
// 指定デバイスのアクティベーションを解除（ライセンス管理ダイアログ用）

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const SUPABASE_URL = Deno.env.get("SUPABASE_URL") || "";
const SUPABASE_SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") || "";

interface RequestBody {
  licenseKey: string;
  hardwareId: string; // 呼び出し元のハードウェアID（認証用）
  activationId: string; // 解除するアクティベーションのID
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
    const { licenseKey, hardwareId, activationId }: RequestBody = await req.json();

    if (!licenseKey || !hardwareId || !activationId) {
      return new Response(
        JSON.stringify({ error: "ライセンスキー、ハードウェアID、アクティベーションIDが必要です" }),
        { status: 400, headers: corsHeaders }
      );
    }

    const supabase = createClient(SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY);

    // ライセンスを検索
    const { data: license, error: licenseError } = await supabase
      .from("licenses")
      .select("id")
      .eq("license_key", licenseKey)
      .eq("is_active", true)
      .single();

    if (licenseError || !license) {
      return new Response(
        JSON.stringify({ error: "ライセンスが見つかりません" }),
        { status: 404, headers: corsHeaders }
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

    // 対象アクティベーションが同じライセンスに属するか確認
    const { data: targetActivation, error: targetError } = await supabase
      .from("license_activations")
      .select("id, license_id")
      .eq("id", activationId)
      .eq("license_id", license.id)
      .eq("is_active", true)
      .single();

    if (targetError || !targetActivation) {
      return new Response(
        JSON.stringify({ error: "指定されたアクティベーションが見つかりません" }),
        { status: 404, headers: corsHeaders }
      );
    }

    // デアクティベート
    const { error: updateError } = await supabase
      .from("license_activations")
      .update({ is_active: false })
      .eq("id", activationId);

    if (updateError) {
      console.error("deactivate-device update error:", updateError);
      return new Response(
        JSON.stringify({ error: updateError.message }),
        { status: 500, headers: corsHeaders }
      );
    }

    // activation_count を更新
    const { count } = await supabase
      .from("license_activations")
      .select("*", { count: "exact", head: true })
      .eq("license_id", license.id)
      .eq("is_active", true);

    if (count !== null) {
      await supabase
        .from("licenses")
        .update({ activation_count: count })
        .eq("id", license.id)
        .then(() => {})
        .catch(() => {});
    }

    return new Response(
      JSON.stringify({ success: true }),
      { headers: corsHeaders }
    );
  } catch (error) {
    console.error("deactivate-device:", error);
    return new Response(
      JSON.stringify({ error: (error as Error).message }),
      { status: 500, headers: corsHeaders }
    );
  }
});

// Supabase Edge Function: verify-license
// ライセンスキーとハードウェアIDを検証する

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "@supabase/supabase-js";
import { normalizeCompactToStorage } from "../_shared/compact-license-key.ts";

const SUPABASE_URL = Deno.env.get("SUPABASE_URL") || "";
const CUSTOM_SERVICE_ROLE_KEY = Deno.env.get("PDFHANDLER_SUPABASE_SERVICE_ROLE_KEY") || "";
const RESERVED_SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") || "";
const SUPABASE_SERVICE_ROLE_KEY = CUSTOM_SERVICE_ROLE_KEY || RESERVED_SERVICE_ROLE_KEY;
const SERVICE_ROLE_KEY_SOURCE = CUSTOM_SERVICE_ROLE_KEY
  ? "PDFHANDLER_SUPABASE_SERVICE_ROLE_KEY"
  : RESERVED_SERVICE_ROLE_KEY
    ? "SUPABASE_SERVICE_ROLE_KEY"
    : "missing";

interface RequestBody {
  licenseKey: string;
  hardwareId: string;
  deviceName?: string; // アクティベーション時の PC 名（Environment.MachineName）
  appVersion?: string; // アプリのバージョン（例: "1.0.0"）、初回アクティベーション時に purchased_version 設定に使用
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

    const { licenseKey, hardwareId, deviceName, appVersion }: RequestBody = await req.json();

    if (!licenseKey || !hardwareId) {
      return new Response(
        JSON.stringify({ error: "ライセンスキーとハードウェアIDが必要です" }),
        {
          status: 400,
          headers: {
            "Content-Type": "application/json",
            "Access-Control-Allow-Origin": "*",
          },
        }
      );
    }

    const supabase = createClient(SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY);

    // ライセンスキーを正規化（4桁区切り入力・表示形式対応）
    const normalizedKey = normalizeLicenseKey(licenseKey) || licenseKey;

    // ライセンスを検索
    const { data: license, error: licenseError } = await supabase
      .from("licenses")
      .select("*")
      .eq("license_key", normalizedKey)
      .single();

    if (licenseError || !license) {
      return new Response(
        JSON.stringify({
          isValid: false,
          errorMessage: "ライセンスが見つかりません",
        }),
        {
          status: 200,
          headers: {
            "Content-Type": "application/json",
            "Access-Control-Allow-Origin": "*",
          },
        }
      );
    }
    if (!license.is_active) {
      return new Response(
        JSON.stringify({
          isValid: false,
          errorMessage: license.revoked_at
            ? "このライセンスは無効化されています"
            : "このライセンスは一時停止中です",
        }),
        {
          status: 200,
          headers: {
            "Content-Type": "application/json",
            "Access-Control-Allow-Origin": "*",
          },
        }
      );
    }

    // 買い切り版の場合は常に有効
    if (license.plan === "purchased") {
      // ハードウェアIDのアクティベーションをチェック・登録
      // 解除済み行が残っている場合は UNIQUE(license_id, hardware_id) のため再 insert せず再有効化する。
      const { data: activation } = await supabase
        .from("license_activations")
        .select("*")
        .eq("license_id", license.id)
        .eq("hardware_id", hardwareId)
        .maybeSingle();

      if (!activation || !activation.is_active) {
        // デバイス数上限チェック（ZipSearch / PictComp も同じ上限を共有）
        const DEVICE_LIMIT = 1;
        const { count: activeCount } = await supabase
          .from("license_activations")
          .select("*", { count: "exact", head: true })
          .eq("license_id", license.id)
          .eq("is_active", true);
        if ((activeCount ?? 0) >= DEVICE_LIMIT) {
          return new Response(
            JSON.stringify({
              isValid: false,
              errorMessage: `アクティベーション上限（${DEVICE_LIMIT}台）に達しています`,
            }),
            {
              status: 200,
              headers: { "Content-Type": "application/json", "Access-Control-Allow-Origin": "*" },
            }
          );
        }

        // 初回または再アクティベーション: purchased_version が未設定なら設定
        let purchasedVersion = license.purchased_version;
        if (!purchasedVersion) {
          purchasedVersion = parsePurchasedVersionFromKey(license.license_key) ?? getMajorFromAppVersion(appVersion) ?? "1";
          await supabase
            .from("licenses")
            .update({ purchased_version: purchasedVersion })
            .eq("id", license.id)
            .then(() => {})
            .catch(() => {});
          license.purchased_version = purchasedVersion;
        }

        const activationPayload = {
          license_id: license.id,
          hardware_id: hardwareId,
          device_name: deviceName ?? null,
          activation_date: new Date().toISOString(),
          last_verification_date: new Date().toISOString(),
          is_active: true,
        };

        const activationResult = activation
          ? await supabase
              .from("license_activations")
              .update({
                ...activationPayload,
                deactivated_at: null,
                reactivated_at: new Date().toISOString(),
              })
              .eq("id", activation.id)
          : await supabase
              .from("license_activations")
              .insert(activationPayload);

        const activationError = activationResult.error;

        if (activationError) {
          console.error("Activation upsert error:", activationError);
          return new Response(
            JSON.stringify({
              isValid: false,
              errorMessage: "アクティベーションの登録に失敗しました",
              debugCode: activationError.code ?? null,
              debugMessage: activationError.message ?? null,
              debugDetails: activationError.details ?? null,
              debugHint: activationError.hint ?? null,
              debugKeySource: SERVICE_ROLE_KEY_SOURCE,
              debugKeyRole: getJwtRole(SUPABASE_SERVICE_ROLE_KEY),
            }),
            {
              status: 200,
              headers: {
                "Content-Type": "application/json",
                "Access-Control-Allow-Origin": "*",
              },
            }
          );
        }

        // activation_countを更新（カラムが存在する場合）
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
            .catch(() => {}); // カラムが存在しない場合は無視
        }
      } else {
        // アクティベーションが存在する場合は最終検証日時を更新（device_name も更新可能）
        const updatePayload: Record<string, unknown> = { last_verification_date: new Date().toISOString() };
        if (deviceName != null) {
          updatePayload.device_name = deviceName;
        }
        await supabase
          .from("license_activations")
          .update(updatePayload)
          .eq("id", activation.id);
      }

      // 最終検証日時を更新
      await supabase
        .from("licenses")
        .update({ last_verification_date: new Date().toISOString() })
        .eq("id", license.id);

      const pv = license.purchased_version ?? parsePurchasedVersionFromKey(license.license_key);
      return new Response(
        JSON.stringify({
          isValid: true,
          plan: license.plan,
          purchasedVersion: pv ?? null,
          expirationDate: license.expiration_date,
          lastVerificationDate: new Date().toISOString(),
          nextVerificationDate: null,
        }),
        {
          headers: {
            "Content-Type": "application/json",
            "Access-Control-Allow-Origin": "*",
          },
        }
      );
    }

    return new Response(
      JSON.stringify({
        isValid: false,
        errorMessage: "無効なライセンスプランです（買い切りライセンスのみ対応）",
      }),
      {
        status: 200,
        headers: {
          "Content-Type": "application/json",
          "Access-Control-Allow-Origin": "*",
        },
      }
    );
  } catch (error) {
    console.error("Error verifying license:", error);
    return new Response(
      JSON.stringify({ error: "Internal server error" }),
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

function getJwtRole(jwt: string): string | null {
  try {
    const parts = jwt.split(".");
    if (parts.length < 2) return null;
    const payload = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = payload.padEnd(payload.length + ((4 - payload.length % 4) % 4), "=");
    const json = JSON.parse(atob(padded));
    return typeof json.role === "string" ? json.role : null;
  } catch {
    return null;
  }
}

/**
 * ライセンスキーを DB 照合形式に正規化（コンパクト区切り → 長い HMAC 形式など）
 */
function normalizeLicenseKey(key: string): string | null {
  if (!key || typeof key !== "string") return null;

  const compact = normalizeCompactToStorage(key);
  if (compact) return compact;

  const trimmed = key.trim().toUpperCase();
  const parts = trimmed.split("-");
  if (parts.length < 3) return null;
  const prefix = parts[0];
  if (!prefix || !/^[A-Z0-9]+$/.test(prefix)) return null;

  const formCode = parts[1];
  if (formCode.length !== 4) return null;

  if (parts.length >= 5) {
    const serialPart = parts.slice(2, -1).join("");
    const hmacPart = parts[parts.length - 1];
    if (serialPart.length === 28 && /^[0-9A-F]+$/.test(serialPart) && /^[0-9A-F]+$/.test(hmacPart)) {
      return `${prefix}-${formCode}-${serialPart}-${hmacPart}`;
    }
  }

  const serialPartLegacy = parts.slice(2).join("");
  if (serialPartLegacy.length === 28 && /^[0-9A-F]+$/.test(serialPartLegacy)) {
    return `${prefix}-${formCode}-${serialPartLegacy}`;
  }

  return null;
}

/** コンパクト（記号32・区切り付き含む）または旧形式から purchased_version */
function parsePurchasedVersionFromKey(key: string): string | null {
  const flat = key.replace(/[\s-]/g, "").toUpperCase();
  if (flat.length === 32 && /^(PDFH|ZIPS|PICT)P[12]\d{2}/.test(flat)) {
    const form = flat.slice(4, 8);
    const m = form.match(/^P([12])(\d{2})$/);
    if (m) {
      const ver = m[2];
      return ver === "00" ? null : String(parseInt(ver, 10));
    }
  }
  const m2 = key.match(/^PDFH-(P[12])(\d{2})-/);
  if (!m2) return null;
  const ver = m2[2];
  return ver === "00" ? null : String(parseInt(ver, 10));
}

/** appVersion "1.0.0" からメジャー "1" を取得 */
function getMajorFromAppVersion(appVersion?: string): string | null {
  if (!appVersion || typeof appVersion !== "string") return null;
  const m = appVersion.trim().match(/^(\d+)/);
  return m ? m[1] : null;
}


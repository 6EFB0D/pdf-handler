// Supabase Edge Function: verify-license
// ライセンスキーとハードウェアIDを検証する

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "@supabase/supabase-js";

const SUPABASE_URL = Deno.env.get("SUPABASE_URL") || "";
const SUPABASE_SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") || "";

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
      .eq("is_active", true)
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

    // 買い切り版の場合は常に有効
    if (license.plan === "purchased") {
      // ハードウェアIDのアクティベーションをチェック・登録
      const { data: activation } = await supabase
        .from("license_activations")
        .select("*")
        .eq("license_id", license.id)
        .eq("hardware_id", hardwareId)
        .eq("is_active", true)
        .single();

      if (!activation) {
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

        // 初回アクティベーション: purchased_version が未設定なら設定
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

        // アクティベーションが存在しない場合は作成
        const { error: activationError } = await supabase
          .from("license_activations")
          .insert({
            license_id: license.id,
            hardware_id: hardwareId,
            device_name: deviceName ?? null,
            activation_date: new Date().toISOString(),
            last_verification_date: new Date().toISOString(),
            is_active: true,
          });

        if (activationError) {
          return new Response(
            JSON.stringify({
              isValid: false,
              errorMessage: "アクティベーションの登録に失敗しました",
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

/**
 * ライセンスキーを正規化（4桁区切り入力→標準形式）
 * PDFH-P101-A1B2-C3D4-...-M3-1A2B → PDFH-P101-A1B2C3D4...-1A2B
 */
function normalizeLicenseKey(key: string): string | null {
  if (!key || typeof key !== "string") return null;
  const trimmed = key.trim().toUpperCase();
  const parts = trimmed.split("-");
  if (parts.length < 3) return null;
  const prefix = parts[0];
  if (!prefix || !/^[A-Z0-9]+$/.test(prefix)) return null;

  const formCode = parts[1];
  if (formCode.length !== 4) return null;

  // HMAC形式: {app_id}-P101-{serial(28)}-{hmac} （最後がHMAC、その前がシリアル）
  if (parts.length >= 5) {
    const serialPart = parts.slice(2, -1).join("");
    const hmacPart = parts[parts.length - 1];
    if (serialPart.length === 28 && /^[0-9A-F]+$/.test(serialPart) && /^[0-9A-F]+$/.test(hmacPart)) {
      return `${prefix}-${formCode}-${serialPart}-${hmacPart}`;
    }
  }

  // 旧形式: {app_id}-P101-28文字（4桁区切り入力時は複数パーツ）
  const serialPartLegacy = parts.slice(2).join("");
  if (serialPartLegacy.length === 28 && /^[0-9A-F]+$/.test(serialPartLegacy)) {
    return `${prefix}-${formCode}-${serialPartLegacy}`;
  }

  return null;
}

/** 新形式 PDFH-P101-xxx から purchased_version を取得。旧形式は null */
function parsePurchasedVersionFromKey(key: string): string | null {
  const m = key.match(/^PDFH-(P[12])(\d{2})-/);
  if (!m) return null;
  const ver = m[2];
  return ver === "00" ? null : String(parseInt(ver, 10));
}

/** appVersion "1.0.0" からメジャー "1" を取得 */
function getMajorFromAppVersion(appVersion?: string): string | null {
  if (!appVersion || typeof appVersion !== "string") return null;
  const m = appVersion.trim().match(/^(\d+)/);
  return m ? m[1] : null;
}


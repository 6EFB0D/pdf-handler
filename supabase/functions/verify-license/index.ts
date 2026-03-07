// Supabase Edge Function: verify-license
// ライセンスキーとハードウェアIDを検証する

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

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

    // ライセンスを検索
    const { data: license, error: licenseError } = await supabase
      .from("licenses")
      .select("*")
      .eq("license_key", licenseKey)
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
              errorMessage: activationError.message,
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
          subscriptionRenewalDate: license.subscription_renewal_date,
          lastVerificationDate: new Date().toISOString(),
          nextVerificationDate: null, // 買い切り版は検証不要
        }),
        {
          headers: {
            "Content-Type": "application/json",
            "Access-Control-Allow-Origin": "*",
          },
        }
      );
    }

    // サブスクリプション版の場合は有効期限をチェック
    if (license.plan === "subscription_standard" || license.plan === "subscription_premium") {
      const now = new Date();
      const renewalDate = license.subscription_renewal_date
        ? new Date(license.subscription_renewal_date)
        : null;

      if (!renewalDate || now > renewalDate) {
        return new Response(
          JSON.stringify({
            isValid: false,
            errorMessage: "サブスクリプションの有効期限が切れています",
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

      // ハードウェアIDのアクティベーションをチェック
      const { data: activation } = await supabase
        .from("license_activations")
        .select("*")
        .eq("license_id", license.id)
        .eq("hardware_id", hardwareId)
        .eq("is_active", true)
        .single();

      if (!activation) {
        // アクティベーションが存在しない場合は作成（デバイス数制限チェックあり）
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
              errorMessage: activationError.message,
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

        // activation_countを更新
        const { count: subCount } = await supabase
          .from("license_activations")
          .select("*", { count: "exact", head: true })
          .eq("license_id", license.id)
          .eq("is_active", true);
        if (subCount !== null) {
          await supabase
            .from("licenses")
            .update({ activation_count: subCount })
            .eq("id", license.id)
            .then(() => {})
            .catch(() => {});
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

      // ライセンスの最終検証日時を更新
      const nextVerificationDate = new Date();
      nextVerificationDate.setDate(nextVerificationDate.getDate() + 30);

      await supabase
        .from("licenses")
        .update({
          last_verification_date: new Date().toISOString(),
        })
        .eq("id", license.id);

      return new Response(
        JSON.stringify({
          isValid: true,
          plan: license.plan,
          purchasedVersion: null, // サブスクは全バージョン
          expirationDate: license.expiration_date,
          subscriptionRenewalDate: license.subscription_renewal_date,
          lastVerificationDate: new Date().toISOString(),
          nextVerificationDate: nextVerificationDate.toISOString(),
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
        errorMessage: "無効なライセンスプランです",
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


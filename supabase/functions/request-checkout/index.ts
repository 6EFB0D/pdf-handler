// Supabase Edge Function: request-checkout
// 「お支払い手続きメール送信」方式の Checkout リクエスト
//
// 旧 create-checkout-session との違い:
//   - Stripe Checkout URL を呼び出し元 (C# アプリ) に返さない
//   - 代わりに Resend で「お支払いリンク入りメール」をユーザーに送信する
//   - レスポンスは { success: true, emailMasked } のみ
//
// メリット:
//   - メールアドレス誤入力を決済前に検知（リンクが届かない時点で気付ける）
//   - 弱い本人確認として機能（受信できた人だけが決済に進める）
//   - 「払ったのに届かない」サポート案件を削減
//
// stripe-node 非使用: fetch + 公式 REST API を使用し Deno / esm の不整合を回避

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";

interface RequestBody {
  plan?: string;
  appId?: string;
  customerEmail?: string;
  majorVersion?: string;
}

const SUPPORT_EMAIL = "support@office-goplan.com";

// ---------------------------------------------------------------------------
// アプリ別設定
// stripe-webhook/index.ts の APP_MAIL_CONFIG と揃える
// ---------------------------------------------------------------------------
interface AppMailConfig {
  appName: string;
  subject: string;
  fromEnvKey: string;
}

const APP_MAIL_CONFIG: Record<string, AppMailConfig> = {
  PDFH: {
    appName: "PDFハンドラ",
    subject: "【PDFハンドラ】お支払い手続きのご案内",
    fromEnvKey: "LICENSE_EMAIL_FROM",
  },
  ZIPS: {
    appName: "ZipSearch",
    subject: "【ZipSearch】お支払い手続きのご案内",
    fromEnvKey: "LICENSE_EMAIL_FROM_ZIPS",
  },
  PICT: {
    appName: "PictComp",
    subject: "【PictComp】お支払い手続きのご案内",
    fromEnvKey: "LICENSE_EMAIL_FROM_PICT",
  },
};

function getAppMailConfig(appId: string): AppMailConfig {
  return APP_MAIL_CONFIG[appId.toUpperCase()] ?? APP_MAIL_CONFIG["PDFH"];
}

// ---------------------------------------------------------------------------
// 共通ヘルパ
// ---------------------------------------------------------------------------
function jsonResponse(status: number, body: Record<string, unknown>): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: {
      "Content-Type": "application/json",
      "Access-Control-Allow-Origin": "*",
    },
  });
}

function checkoutBaseUrls(): { success_url: string; cancel_url: string } {
  const raw = (Deno.env.get("APP_URL") ?? "").trim().replace(/\/+$/, "");
  if (!raw) {
    throw new Error("APP_URL が未設定です（Edge Functions の Secrets を確認）");
  }
  const base = raw;
  let parsed: URL;
  try {
    parsed = new URL(base);
  } catch {
    throw new Error(`APP_URL が URL として無効です: "${raw}"`);
  }
  if (parsed.protocol !== "http:" && parsed.protocol !== "https:") {
    throw new Error(`APP_URL は http(s) である必要があります: "${base}"`);
  }
  return {
    success_url: `${base}/success?session_id={CHECKOUT_SESSION_ID}`,
    cancel_url: `${base}/cancel`,
  };
}

function isValidEmail(s: string): boolean {
  const t = s.trim();
  if (t.length < 3 || t.length > 254) return false;
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(t);
}

/** C# / JSON のキー揺れを吸収 */
function readCustomerEmail(payload: Record<string, unknown>): string {
  const raw = payload["customerEmail"] ?? payload["CustomerEmail"];
  return typeof raw === "string" ? raw.trim() : "";
}

function maskEmail(email: string): string {
  const at = email.indexOf("@");
  return at > 0 ? `${email.slice(0, 1)}***${email.slice(at)}` : "***";
}

// ---------------------------------------------------------------------------
// Stripe API: Customer / Checkout Session 作成
// ---------------------------------------------------------------------------

/** 指定メールの Customer を作成して ID を返す
 *  （customer_balance = 銀行振込を使うには Checkout 前に customer の事前作成が必須） */
async function createCustomer(apiKey: string, email: string): Promise<string> {
  const body = new URLSearchParams();
  body.set("email", email);

  const res = await fetch("https://api.stripe.com/v1/customers", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${apiKey}`,
      "Content-Type": "application/x-www-form-urlencoded",
    },
    body,
  });
  const data = (await res.json()) as {
    id?: string;
    error?: { message?: string; type?: string };
  };
  if (!res.ok || !data.id) {
    const msg = data?.error?.message ?? `HTTP ${res.status}`;
    const typ = data?.error?.type ?? "stripe_error";
    throw new Error(`customer_create: ${typ}: ${msg}`);
  }
  return data.id;
}

/** https://docs.stripe.com/api/checkout/sessions/create */
async function createCheckoutSession(params: {
  apiKey: string;
  priceId: string;
  success_url: string;
  cancel_url: string;
  customerId: string;
  enableJpBankTransfer: boolean;
  metadata: { plan: string; app_id: string; major_version: string };
}): Promise<{ url: string | null }> {
  const body = new URLSearchParams();
  body.set("mode", "payment");
  body.set("success_url", params.success_url);
  body.set("cancel_url", params.cancel_url);
  body.set("customer", params.customerId);
  body.set("locale", "ja");
  body.set("line_items[0][price]", params.priceId);
  body.set("line_items[0][quantity]", "1");
  body.set("metadata[plan]",          params.metadata.plan);
  body.set("metadata[app_id]",        params.metadata.app_id);
  body.set("metadata[major_version]", params.metadata.major_version);

  // 支払方法：既定はカードのみ。銀行振込は Stripe 側で有効化済みの場合だけ追加する。
  body.set("payment_method_types[0]", "card");
  if (params.enableJpBankTransfer) {
    body.set("payment_method_types[1]", "customer_balance");
    body.set("payment_method_options[customer_balance][funding_type]", "bank_transfer");
    body.set("payment_method_options[customer_balance][bank_transfer][type]", "jp_bank_transfer");
  }

  // 請求書（Invoice）の自動発行を有効化
  body.set("invoice_creation[enabled]", "true");
  body.set("invoice_creation[invoice_data][metadata][app_id]",        params.metadata.app_id);
  body.set("invoice_creation[invoice_data][metadata][major_version]", params.metadata.major_version);

  // 有効期限は Stripe デフォルト (24h)。明示指定は不要。

  const res = await fetch("https://api.stripe.com/v1/checkout/sessions", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${params.apiKey}`,
      "Content-Type": "application/x-www-form-urlencoded",
    },
    body,
  });

  const data = (await res.json()) as {
    url?: string | null;
    error?: { message?: string; type?: string };
  };

  if (!res.ok) {
    const msg = data?.error?.message ?? `HTTP ${res.status}`;
    const typ = data?.error?.type ?? "stripe_error";
    throw new Error(`${typ}: ${msg}`);
  }

  return { url: data.url ?? null };
}

// ---------------------------------------------------------------------------
// Resend: 決済リンクメール送信
// ---------------------------------------------------------------------------
async function sendCheckoutLinkEmail(
  toEmail: string,
  appId: string,
  checkoutUrl: string,
): Promise<void> {
  const apiKey = (Deno.env.get("RESEND_API_KEY") ?? "").trim();
  if (!apiKey) {
    throw new Error("RESEND_API_KEY が未設定です");
  }

  const cfg = getAppMailConfig(appId);
  const from =
    Deno.env.get(cfg.fromEnvKey) ||
    Deno.env.get("LICENSE_EMAIL_FROM") ||
    `${cfg.appName} <onboarding@resend.dev>`;

  // 参照番号表示用のメタ（本文に含める）
  const infoRow = (label: string, value: string) =>
    `<tr>
       <td style="padding:6px 14px 6px 0;vertical-align:top;font-weight:bold;color:#555;white-space:nowrap">${label}</td>
       <td style="padding:6px 0;vertical-align:top;color:#333;word-break:break-all">${value}</td>
     </tr>`;

  const infoBlock = `
    <table role="presentation" cellpadding="0" cellspacing="0" border="0"
           style="width:100%;margin:16px 0;padding:14px 16px;
                  background:#f8f9fb;border-left:4px solid #3b82f6;
                  border-radius:6px;border-collapse:separate;font-size:13px">
      ${infoRow("商品", `${cfg.appName} Standard 版（買い切り）`)}
      ${infoRow("お支払い方法", "クレジットカード または 銀行振込（日本）")}
      ${infoRow("リンク有効期限", "本メール送信から 24 時間")}
    </table>`;

  const html = `
<!DOCTYPE html>
<html lang="ja">
<head><meta charset="utf-8"><title>お支払い手続きのご案内</title></head>
<body style="font-family:'Meiryo',sans-serif;line-height:1.7;color:#333;max-width:560px;margin:0 auto;padding:24px">
  <h2 style="color:#1a237e">${cfg.appName} お支払い手続きのご案内</h2>

  <p style="margin:0 0 16px">お客様</p>

  <p>この度は ${cfg.appName} Standard 版（買い切り）のお申込みをいただきありがとうございます。</p>
  <p>以下のボタンよりお支払い手続きにお進みください。</p>

  <p style="text-align:center;margin:28px 0">
    <a href="${checkoutUrl}"
       style="display:inline-block;padding:14px 28px;
              background:#1565c0;color:#ffffff;text-decoration:none;
              font-weight:bold;border-radius:6px;font-size:15px">
      お支払いページへ進む
    </a>
  </p>

  <p style="font-size:12px;color:#666;margin:0 0 6px">ボタンが機能しない場合は、以下の URL をブラウザに貼り付けてください:</p>
  <p style="font-size:12px;color:#1565c0;word-break:break-all;margin:0 0 16px">
    <a href="${checkoutUrl}" style="color:#1565c0">${checkoutUrl}</a>
  </p>

  ${infoBlock}

  <p style="font-size:13px;color:#555;line-height:1.7">
    ◆ ご注意<br>
    ・本リンクの有効期限は送信から <strong>24 時間</strong> です。<br>
    ・期限が切れた場合は、アプリから再度お申込みください。<br>
    ・このメールに心当たりがない場合は破棄してください。決済は行われません。
  </p>

  <hr style="margin:24px 0;border:none;border-top:1px solid #ddd">

  <p style="font-size:12px;color:#999;margin-top:16px;line-height:1.6">
    このメールは自動送信されています。ご不明な点はサポート
    (<a href="mailto:${SUPPORT_EMAIL}" style="color:#666">${SUPPORT_EMAIL}</a>)
    までご連絡ください。
  </p>
</body>
</html>`;

  const res = await fetch("https://api.resend.com/emails", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "Authorization": `Bearer ${apiKey}`,
    },
    body: JSON.stringify({
      from,
      to: [toEmail],
      subject: cfg.subject,
      html,
    }),
  });

  if (!res.ok) {
    const errBody = await res.text();
    throw new Error(`Resend API error ${res.status}: ${errBody}`);
  }
}

// ---------------------------------------------------------------------------
// エントリポイント
// ---------------------------------------------------------------------------
serve(async (req) => {
  try {
    if (req.method === "OPTIONS") {
      return new Response(null, {
        headers: {
          "Access-Control-Allow-Origin": "*",
          "Access-Control-Allow-Methods": "POST, OPTIONS",
          "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
        },
      });
    }

    let payload: RequestBody;
    try {
      payload = await req.json();
    } catch {
      return jsonResponse(400, { error: "JSON ボディが不正です" });
    }

    const plan = typeof payload?.plan === "string" ? payload.plan.trim() : "";
    const app_id = (typeof payload?.appId === "string" ? payload.appId.trim() : "").toUpperCase() || "PDFH";

    if (plan !== "StandardPurchased") {
      return jsonResponse(400, { error: "無効なプランです" });
    }

    const customerEmail = readCustomerEmail(payload as Record<string, unknown>);
    if (!customerEmail || !isValidEmail(customerEmail)) {
      return jsonResponse(400, { error: "customerEmail が無効です" });
    }

    // 本番デプロイ確認用（全文はログに残さない）
    const emailMask = maskEmail(customerEmail);
    console.log(
      `request-checkout: plan=${plan} app_id=${app_id} customerEmail_mask=${emailMask}`,
    );

    const apiKey = (Deno.env.get("STRIPE_SECRET_KEY") ?? "").trim();
    if (!apiKey) {
      return jsonResponse(500, { error: "STRIPE_SECRET_KEY が未設定です（Edge Functions の Secrets を確認）" });
    }

    // アプリ別 Price ID を優先し、未設定なら共通 ID にフォールバック
    // 例: STRIPE_PRICE_ID_PDFH_PURCHASED / STRIPE_PRICE_ID_ZIPS_PURCHASED / STRIPE_PRICE_ID_PICT_PURCHASED
    const priceEnvKey = `STRIPE_PRICE_ID_${app_id}_PURCHASED`;
    const priceId = (Deno.env.get(priceEnvKey) ?? Deno.env.get("STRIPE_PRICE_ID_PURCHASED") ?? "").trim();
    if (!priceId) {
      return jsonResponse(500, {
        error: "買い切り用の Price ID が設定されていません",
      });
    }

    const { success_url, cancel_url } = checkoutBaseUrls();

    // バージョン：リクエスト body → 環境変数の順で取得
    const major_version = (
      typeof (payload as any)?.majorVersion === "string"
        ? (payload as any).majorVersion.trim()
        : ""
    ) || (Deno.env.get("LICENSE_PURCHASED_MAJOR_VERSION") ?? "1");

    const enableJpBankTransfer = (Deno.env.get("STRIPE_ENABLE_JP_BANK_TRANSFER") ?? "")
      .trim()
      .toLowerCase() === "true";

    // Customer はカード決済でも再利用・領収書管理に使えるため作成する。
    // 銀行振込(customer_balance)を有効化する場合も Checkout 前の customer 指定が必要。
    const customerId = await createCustomer(apiKey, customerEmail);

    const session = await createCheckoutSession({
      apiKey,
      priceId,
      success_url,
      cancel_url,
      customerId,
      enableJpBankTransfer,
      metadata: { plan, app_id, major_version },
    });

    if (!session.url) {
      return jsonResponse(500, { error: "Stripe が checkout url を返しませんでした" });
    }

    // ★ 重要: Checkout URL は呼び出し元 (C# アプリ) には返さず、メール送信のみ行う
    await sendCheckoutLinkEmail(customerEmail, app_id, session.url);

    console.log(`request-checkout: email sent to ${emailMask} (session=${session.id ?? "?"})`);

    return jsonResponse(200, {
      success: true,
      emailMasked: emailMask,
      message: "お支払い用のメールを送信しました",
    });
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    console.error("Error in request-checkout:", message, error);
    // デバッグ時のみ詳細をレスポンスに含める
    // （本番安定後は "Internal server error" に戻す）
    return jsonResponse(500, {
      error: "Internal server error",
      detail: message,
    });
  }
});

// Supabase Edge Function: stripe-webhook
// Stripe Webhook（買い切り checkout のみ）

import Stripe from "stripe";
import { createClient } from "@supabase/supabase-js";
import { generateCompactLicenseKey } from "../_shared/compact-license-key.ts";

const stripe = new Stripe(Deno.env.get("STRIPE_SECRET_KEY") || "", {
  apiVersion: "2023-10-16",
  httpClient: Stripe.createFetchHttpClient(),
});

const SUPABASE_URL = Deno.env.get("SUPABASE_URL") || "";
const SUPABASE_SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") || "";
const STRIPE_WEBHOOK_SECRET = Deno.env.get("STRIPE_WEBHOOK_SECRET") || "";
const RESEND_API_KEY = Deno.env.get("RESEND_API_KEY") || "";
const SUPPORT_EMAIL = "support@office-goplan.com";

Deno.serve(async (req) => {
  try {
    if (req.method !== "POST") {
      return new Response(JSON.stringify({ error: "Method not allowed" }), {
        status: 405,
        headers: { "Content-Type": "application/json" },
      });
    }

    const signature = req.headers.get("stripe-signature");
    if (!signature) {
      return new Response(JSON.stringify({ error: "Stripe signature not found" }), {
        status: 400,
        headers: { "Content-Type": "application/json", "Access-Control-Allow-Origin": "*" },
      });
    }

    const body = await req.text();

    let event: Stripe.Event;
    try {
      event = await stripe.webhooks.constructEventAsync(body, signature, STRIPE_WEBHOOK_SECRET);
    } catch (err) {
      console.error("Webhook signature verification failed:", err.message);
      return new Response(
        JSON.stringify({ error: "Invalid signature" }),
        { status: 400, headers: { "Content-Type": "application/json" } },
      );
    }

    const supabase = createClient(SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY);

    if (event.type === "checkout.session.completed") {
      const session = event.data.object as Stripe.Checkout.Session;
      if (session.payment_status === "paid") {
        // カード決済：即ライセンス発行
        await handleCheckoutCompleted(supabase, session);
      } else {
        // 銀行振込等：未入金のためライセンス発行せず、管理者通知のみ
        console.log(`Pending payment detected: session=${session.id} status=${session.payment_status}`);
        await handlePendingPayment(session);
      }
    } else if (event.type === "checkout.session.async_payment_succeeded") {
      // 銀行振込の入金確認：ライセンス発行
      const session = event.data.object as Stripe.Checkout.Session;
      console.log(`Async payment succeeded: session=${session.id}`);
      await handleCheckoutCompleted(supabase, session);
    } else if (event.type === "checkout.session.async_payment_failed") {
      // 銀行振込の入金失敗（期限切れ等）
      const session = event.data.object as Stripe.Checkout.Session;
      console.warn(`Async payment failed: session=${session.id}`);
      await handleAsyncPaymentFailed(session);
    } else {
      console.log(`Ignored event type: ${event.type}`);
    }

    return new Response(JSON.stringify({ received: true }), {
      headers: { "Content-Type": "application/json" },
    });
  } catch (error) {
    console.error("Webhook error:", error.message, error.stack);
    return new Response(JSON.stringify({ error: "Internal server error" }), {
      status: 500,
      headers: { "Content-Type": "application/json" },
    });
  }
});

// ---------------------------------------------------------------------------
// 内部監視メール（support 宛）
// ---------------------------------------------------------------------------

/** support@office-goplan.com へ通知メールを送信する共通関数 */
async function sendAdminNotification(subject: string, html: string): Promise<void> {
  if (!RESEND_API_KEY) return;
  try {
    const res = await fetch("https://api.resend.com/emails", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${RESEND_API_KEY}`,
      },
      body: JSON.stringify({
        from: `Office Go Plan システム <${SUPPORT_EMAIL}>`,
        to: [SUPPORT_EMAIL],
        subject,
        html,
      }),
    });
    if (!res.ok) console.error("Admin notification failed:", await res.text());
  } catch (e) {
    console.error("Admin notification error:", e);
  }
}

function now(): string {
  return new Date().toLocaleString("ja-JP", { timeZone: "Asia/Tokyo" });
}

/** ✅ 購入完了通知 */
function buildSuccessNotification(params: {
  userEmail: string;
  appId: string;
  licenseKey: string;
  sessionId: string;
  paymentIntentId: string | null;
  emailSent: boolean;
}): { subject: string; html: string } {
  const subject = `✅ 購入完了 | ${params.appId} | ${params.userEmail}`;
  const emailStatus = params.emailSent
    ? `<span style="color:green">✅ 送信済み</span>`
    : `<span style="color:orange">⚠️ 未送信（手動対応が必要）</span>`;
  const html = `
<div style="font-family:sans-serif;max-width:600px;color:#333">
  <h2 style="color:#2e7d32">✅ 購入完了・ライセンス発行</h2>
  <table style="border-collapse:collapse;width:100%">
    <tr><td style="padding:6px 12px;background:#f5f5f5;width:160px"><strong>日時</strong></td>
        <td style="padding:6px 12px">${now()}</td></tr>
    <tr><td style="padding:6px 12px;background:#f5f5f5"><strong>顧客メール</strong></td>
        <td style="padding:6px 12px">${params.userEmail}</td></tr>
    <tr><td style="padding:6px 12px;background:#f5f5f5"><strong>アプリ</strong></td>
        <td style="padding:6px 12px">${params.appId}</td></tr>
    <tr><td style="padding:6px 12px;background:#f5f5f5"><strong>ライセンスキー</strong></td>
        <td style="padding:6px 12px;font-family:monospace;font-size:12px;word-break:break-all">${params.licenseKey}</td></tr>
    <tr><td style="padding:6px 12px;background:#f5f5f5"><strong>顧客へのメール</strong></td>
        <td style="padding:6px 12px">${emailStatus}</td></tr>
    <tr><td style="padding:6px 12px;background:#f5f5f5"><strong>Checkout Session</strong></td>
        <td style="padding:6px 12px;font-family:monospace;font-size:11px">${params.sessionId}</td></tr>
    <tr><td style="padding:6px 12px;background:#f5f5f5"><strong>Payment Intent</strong></td>
        <td style="padding:6px 12px;font-family:monospace;font-size:11px">${params.paymentIntentId ?? "（なし）"}</td></tr>
  </table>
</div>`;
  return { subject, html };
}

/** ⏳ 銀行振込「入金待ち」通知 */
function buildPendingNotification(params: {
  userEmail: string;
  appId: string;
  sessionId: string;
  amountTotal: number | null;
  currency: string | null;
}): { subject: string; html: string } {
  const subject = `⏳ 銀行振込・入金待ち | ${params.appId} | ${params.userEmail}`;
  const amount = params.amountTotal != null
    ? `${(params.amountTotal).toLocaleString()} ${(params.currency ?? "jpy").toUpperCase()}`
    : "（不明）";
  const html = `
<div style="font-family:sans-serif;max-width:600px;color:#333">
  <h2 style="color:#ed6c02">⏳ 銀行振込・入金待ち</h2>
  <p>お客様が銀行振込を選択しました。Stripeから振込先案内メールが自動送信されています。</p>
  <p>入金確認後、自動でライセンスが発行されます。</p>
  <table style="border-collapse:collapse;width:100%">
    <tr><td style="padding:6px 12px;background:#fff8e1;width:160px"><strong>日時</strong></td>
        <td style="padding:6px 12px">${now()}</td></tr>
    <tr><td style="padding:6px 12px;background:#fff8e1"><strong>顧客メール</strong></td>
        <td style="padding:6px 12px">${params.userEmail}</td></tr>
    <tr><td style="padding:6px 12px;background:#fff8e1"><strong>アプリ</strong></td>
        <td style="padding:6px 12px">${params.appId}</td></tr>
    <tr><td style="padding:6px 12px;background:#fff8e1"><strong>金額</strong></td>
        <td style="padding:6px 12px">${amount}</td></tr>
    <tr><td style="padding:6px 12px;background:#fff8e1"><strong>Checkout Session</strong></td>
        <td style="padding:6px 12px;font-family:monospace;font-size:11px">${params.sessionId}</td></tr>
  </table>
</div>`;
  return { subject, html };
}

/** ⚠️ 銀行振込「入金失敗」通知 */
function buildPaymentFailedNotification(params: {
  userEmail: string;
  appId: string;
  sessionId: string;
}): { subject: string; html: string } {
  const subject = `⚠️ 入金失敗・期限切れ | ${params.appId} | ${params.userEmail}`;
  const html = `
<div style="font-family:sans-serif;max-width:600px;color:#333">
  <h2 style="color:#c62828">⚠️ 銀行振込・入金失敗</h2>
  <p>期限内に入金が確認できなかったため、Stripeのチェックアウトが失効しました。</p>
  <p>必要に応じて顧客へ連絡してください。</p>
  <table style="border-collapse:collapse;width:100%">
    <tr><td style="padding:6px 12px;background:#fff3f3;width:160px"><strong>日時</strong></td>
        <td style="padding:6px 12px">${now()}</td></tr>
    <tr><td style="padding:6px 12px;background:#fff3f3"><strong>顧客メール</strong></td>
        <td style="padding:6px 12px">${params.userEmail}</td></tr>
    <tr><td style="padding:6px 12px;background:#fff3f3"><strong>アプリ</strong></td>
        <td style="padding:6px 12px">${params.appId}</td></tr>
    <tr><td style="padding:6px 12px;background:#fff3f3"><strong>Checkout Session</strong></td>
        <td style="padding:6px 12px;font-family:monospace;font-size:11px">${params.sessionId}</td></tr>
  </table>
</div>`;
  return { subject, html };
}

/** ❌ エラー通知 */
function buildErrorNotification(params: {
  userEmail: string;
  appId: string;
  sessionId: string;
  errorMessage: string;
}): { subject: string; html: string } {
  const subject = `❌ エラー | stripe-webhook | ${params.userEmail}`;
  const html = `
<div style="font-family:sans-serif;max-width:600px;color:#333">
  <h2 style="color:#c62828">❌ Webhook 処理エラー</h2>
  <p style="color:#c62828">ライセンスが発行されていない可能性があります。至急確認してください。</p>
  <table style="border-collapse:collapse;width:100%">
    <tr><td style="padding:6px 12px;background:#fff3f3;width:160px"><strong>日時</strong></td>
        <td style="padding:6px 12px">${now()}</td></tr>
    <tr><td style="padding:6px 12px;background:#fff3f3"><strong>顧客メール</strong></td>
        <td style="padding:6px 12px">${params.userEmail}</td></tr>
    <tr><td style="padding:6px 12px;background:#fff3f3"><strong>アプリ</strong></td>
        <td style="padding:6px 12px">${params.appId}</td></tr>
    <tr><td style="padding:6px 12px;background:#fff3f3"><strong>エラー内容</strong></td>
        <td style="padding:6px 12px;color:#c62828">${params.errorMessage}</td></tr>
    <tr><td style="padding:6px 12px;background:#fff3f3"><strong>Checkout Session</strong></td>
        <td style="padding:6px 12px;font-family:monospace;font-size:11px">${params.sessionId}</td></tr>
  </table>
  <p style="margin-top:16px">
    <a href="https://supabase.com/dashboard/project/_/functions" style="color:#1565c0">
      → Supabase Edge Function ログを確認
    </a>
  </p>
</div>`;
  return { subject, html };
}

// ---------------------------------------------------------------------------
// アプリ別メール設定
// 各アプリの fromEnvKey が未設定の場合は共通の LICENSE_EMAIL_FROM にフォールバック
// ---------------------------------------------------------------------------
interface AppMailConfig {
  appName: string;
  subject: string;
  fromEnvKey: string;
}

const APP_MAIL_CONFIG: Record<string, AppMailConfig> = {
  PDFH: {
    appName: "PDFハンドラ",
    subject: "【PDFハンドラ】ライセンスキー",
    fromEnvKey: "LICENSE_EMAIL_FROM",
  },
  ZIPS: {
    appName: "ZipSearch",
    subject: "【ZipSearch】ライセンスキー",
    fromEnvKey: "LICENSE_EMAIL_FROM_ZIPS",
  },
  PICT: {
    appName: "PictComp",
    subject: "【PictComp】ライセンスキー",
    fromEnvKey: "LICENSE_EMAIL_FROM_PICT",
  },
};

function getAppMailConfig(appId: string): AppMailConfig {
  return APP_MAIL_CONFIG[appId.toUpperCase()] ?? APP_MAIL_CONFIG["PDFH"];
}

// ---------------------------------------------------------------------------

/** 買い切り Standard：コンパクトキー（記号32・Supabase は4桁区切りで保存） */
async function generateLicenseKey(appId: string): Promise<string> {
  const raw = (appId || "PDFH").toUpperCase().replace(/[^A-Z0-9]/g, "");
  const prefix = raw.length >= 4 ? raw.slice(0, 4) : "PDFH";
  const majorVer = Deno.env.get("LICENSE_PURCHASED_MAJOR_VERSION") || "1";
  const formCode = `P1${majorVer.padStart(2, "0")}`;
  const secretKey = Deno.env.get("LICENSE_SECRET_KEY") || undefined;
  const { storageKey } = await generateCompactLicenseKey(secretKey, prefix, formCode);
  return storageKey;
}

async function handleCheckoutCompleted(supabase: any, session: Stripe.Checkout.Session) {
  const plan = session.metadata?.plan || "";
  const appId = (session.metadata?.app_id || "PDFH").toString().trim().toUpperCase() || "PDFH";
  const stripeCheckoutSessionId = session.id;

  if (plan !== "StandardPurchased") {
    console.warn(`Skipping unknown plan: ${plan}`);
    return;
  }

  // 担当者名：カード名義人 / 口座名義人をそのまま使用
  const customerContact = session.customer_details?.name?.trim() || null;
  const customerCompany: string | null = null;

  let stripeCustomerId: string | null = typeof session.customer === "string"
    ? session.customer
    : session.customer?.id ?? null;
  let stripePaymentIntentId: string | null = typeof session.payment_intent === "string"
    ? session.payment_intent
    : session.payment_intent?.id ?? null;

  if (!stripeCustomerId && stripePaymentIntentId) {
    try {
      const paymentIntent = await stripe.paymentIntents.retrieve(stripePaymentIntentId);
      stripeCustomerId = paymentIntent.customer as string | null ?? null;
    } catch (e) {
      console.warn("Could not retrieve customer from payment intent:", e);
    }
  }

  // 冪等性チェック: 同一 payment_intent_id または checkout_session_id で既存なら二重登録をスキップ
  // payment_intent_id が null になる決済手段（銀行振込等）は session.id で代替
  const idempotencyCheck = stripePaymentIntentId
    ? { column: "stripe_payment_intent_id", value: stripePaymentIntentId }
    : { column: "stripe_checkout_session_id", value: stripeCheckoutSessionId };
  const { data: existing } = await supabase
    .from("licenses")
    .select("id")
    .eq(idempotencyCheck.column, idempotencyCheck.value)
    .maybeSingle();
  if (existing) {
    console.log(`Duplicate event skipped: ${idempotencyCheck.column}=${idempotencyCheck.value}`);
    return;
  }

  const licenseKey = await generateLicenseKey(appId);
  // バージョンは Stripe 商品メタデータ (major_version) を優先。
  // 未設定の場合は環境変数 LICENSE_PURCHASED_MAJOR_VERSION にフォールバック。
  const majorVer = session.metadata?.major_version
    ?? Deno.env.get("LICENSE_PURCHASED_MAJOR_VERSION")
    ?? "1";

  const licenseData: Record<string, unknown> = {
    license_key:                licenseKey,
    app_id:                     appId,
    plan:                       "purchased",
    user_email:                 session.customer_email || session.customer_details?.email || "",
    stripe_customer_id:         stripeCustomerId,
    stripe_payment_intent_id:   stripePaymentIntentId,
    stripe_checkout_session_id: stripeCheckoutSessionId,
    activation_date:            new Date().toISOString(),
    is_active:                  true,
    expiration_date:            null,
    purchased_version:          majorVer,
    payment_type:               "stripe",
    customer_contact:           customerContact,   // 担当者名・お名前
    customer_company:           customerCompany,   // 法人名・部署名
  };

  const { data: license, error: licenseError } = await supabase
    .from("licenses")
    .insert(licenseData)
    .select()
    .single();

  if (licenseError) {
    const errMsg = licenseError.message ?? JSON.stringify(licenseError);
    console.error("License insert error:", errMsg);
    // ❌ エラー通知をサポートへ送信
    const { subject, html } = buildErrorNotification({
      userEmail: licenseData.user_email as string,
      appId,
      sessionId: stripeCheckoutSessionId,
      errorMessage: errMsg,
    });
    await sendAdminNotification(subject, html);
    throw new Error(`Failed to create license: ${errMsg}`);
  }

  const userEmail = licenseData.user_email as string;

  // 請求書PDF URLを取得（invoice_creation が有効なら session.invoice がある）
  // 銀行振込の場合、Charge は存在しないが Invoice は必ず存在するので、
  // Invoice 経由で領収書URL（hosted_invoice_url）も取得する
  let invoicePdfUrl: string | null = null;
  let invoiceHostedUrl: string | null = null;
  if (session.invoice) {
    try {
      const invoiceId = typeof session.invoice === "string" ? session.invoice : session.invoice.id;
      const inv = await stripe.invoices.retrieve(invoiceId);
      invoicePdfUrl    = inv.invoice_pdf ?? null;
      invoiceHostedUrl = inv.hosted_invoice_url ?? null;
    } catch (e) {
      console.warn("Could not retrieve invoice PDF URL:", e);
    }
  }

  // 領収書URLを取得
  //   カード決済: Charge.receipt_url を使用
  //   銀行振込:   Charge が存在しないので Invoice.hosted_invoice_url で代用
  let receiptUrl: string | null = null;
  if (stripePaymentIntentId) {
    try {
      const pi = await stripe.paymentIntents.retrieve(stripePaymentIntentId, {
        expand: ["latest_charge"],
      });
      const latestCharge = pi.latest_charge as Stripe.Charge | null;
      receiptUrl = latestCharge?.receipt_url ?? null;
    } catch (e) {
      console.warn("Could not retrieve receipt URL from charge:", e);
    }
  }
  // Charge 経由で取得できなかった場合は Invoice の hosted URL を使用
  if (!receiptUrl && invoiceHostedUrl) {
    receiptUrl = invoiceHostedUrl;
  }

  let emailSent = false;
  if (userEmail && RESEND_API_KEY) {
    try {
      await sendLicenseEmail(userEmail, licenseKey, appId, {
        paymentIntentId:   stripePaymentIntentId,
        checkoutSessionId: stripeCheckoutSessionId,
        receiptUrl,
        invoicePdfUrl,
        customerContact,
        customerCompany,
      });
      emailSent = true;
      console.log(`License email sent to ${userEmail}`);
    } catch (emailErr) {
      console.error("License email send failed:", emailErr);
    }
  } else {
    console.log(`License created: ${licenseKey} for ${userEmail || "(no email)"}`);
  }

  // ✅ 購入完了通知をサポートへ送信
  const { subject, html } = buildSuccessNotification({
    userEmail,
    appId,
    licenseKey,
    sessionId: stripeCheckoutSessionId,
    paymentIntentId: stripePaymentIntentId,
    emailSent,
  });
  await sendAdminNotification(subject, html);
}

// ---------------------------------------------------------------------------
// 銀行振込・非同期決済用ハンドラ
// ---------------------------------------------------------------------------

/** 銀行振込の Checkout 完了（入金前）：管理者通知のみ */
async function handlePendingPayment(session: Stripe.Checkout.Session): Promise<void> {
  const appId = (session.metadata?.app_id || "PDFH").toString().trim().toUpperCase() || "PDFH";
  const userEmail = session.customer_email || session.customer_details?.email || "";
  const { subject, html } = buildPendingNotification({
    userEmail,
    appId,
    sessionId: session.id,
    amountTotal: session.amount_total ?? null,
    currency: session.currency ?? null,
  });
  await sendAdminNotification(subject, html);
}

/** 銀行振込の入金失敗：管理者通知のみ（ライセンスは発行されていないのでDB操作なし） */
async function handleAsyncPaymentFailed(session: Stripe.Checkout.Session): Promise<void> {
  const appId = (session.metadata?.app_id || "PDFH").toString().trim().toUpperCase() || "PDFH";
  const userEmail = session.customer_email || session.customer_details?.email || "";
  const { subject, html } = buildPaymentFailedNotification({
    userEmail,
    appId,
    sessionId: session.id,
  });
  await sendAdminNotification(subject, html);
}

async function sendLicenseEmail(
  toEmail: string,
  licenseKey: string,
  appId: string,
  refs: {
    paymentIntentId:  string | null;
    checkoutSessionId: string;
    receiptUrl:       string | null;
    invoicePdfUrl?:   string | null;
    customerContact?: string | null;
    customerCompany?: string | null;
  },
): Promise<void> {
  const apiKey = Deno.env.get("RESEND_API_KEY");
  const cfg    = getAppMailConfig(appId);

  const from =
    Deno.env.get(cfg.fromEnvKey) ||
    Deno.env.get("LICENSE_EMAIL_FROM") ||
    `${cfg.appName} <onboarding@resend.dev>`;

  // お問い合わせ参照番号（サポート対応用）
  const refNo       = refs.paymentIntentId ?? refs.checkoutSessionId;

  // 参照番号・書類リンクを1つの table にまとめる
  // （Gmail の自動折りたたみ対策：連続する <p> や署名っぽいパターンを避ける）
  const tdLabel = `style="padding:6px 14px 6px 0;vertical-align:top;font-weight:bold;color:#555;white-space:nowrap"`;
  const tdValue = `style="padding:6px 0;vertical-align:top;color:#333;word-break:break-all"`;

  const rows: string[] = [];
  rows.push(`<tr><td ${tdLabel}>お問い合わせ参照番号</td><td ${tdValue}>${refNo}</td></tr>`);
  if (refs.invoicePdfUrl) {
    rows.push(`<tr><td ${tdLabel}>請求書</td><td ${tdValue}><a href="${refs.invoicePdfUrl}" style="color:#1565c0">請求書をダウンロード (PDF)</a></td></tr>`);
  }
  if (refs.receiptUrl) {
    rows.push(`<tr><td ${tdLabel}>領収書</td><td ${tdValue}><a href="${refs.receiptUrl}" style="color:#1565c0">領収書をダウンロード</a></td></tr>`);
  }
  const infoBlock =
    `<table role="presentation" cellpadding="0" cellspacing="0" border="0"
            style="width:100%;margin:16px 0;padding:14px 16px;
                   background:#f8f9fb;border-left:4px solid #3b82f6;
                   border-radius:6px;border-collapse:separate;font-size:13px">
       ${rows.join("\n")}
     </table>`;

  // 宛名（法人名 + 担当者名）
  const companyLine  = refs.customerCompany
    ? `<p style="margin:0 0 2px">${refs.customerCompany}</p>`
    : "";
  const contactLine  = refs.customerContact
    ? `<p style="margin:0 0 16px">${refs.customerContact} 様</p>`
    : `<p style="margin:0 0 16px">お客様</p>`;

  const html = `
<!DOCTYPE html>
<html lang="ja">
<head><meta charset="utf-8"><title>ライセンスキー</title></head>
<body style="font-family:'Meiryo',sans-serif;line-height:1.7;color:#333;max-width:560px;margin:0 auto;padding:24px">
  <h2 style="color:#1a237e">${cfg.appName} ライセンスキー</h2>
  ${companyLine}${contactLine}
  <p>この度は ${cfg.appName} をご購入いただきありがとうございます。</p>
  <p><strong>プラン:</strong> Standard版（買い切り）</p>

  <p style="margin-top:16px"><strong>ライセンスキー:</strong></p>
  <p style="font-family:monospace;background:#f5f5f5;padding:14px;border-radius:6px;
            font-size:13px;word-break:break-all;border-left:4px solid #3b82f6">${licenseKey}</p>

  <div style="margin:20px 0;padding:14px 16px;
              background:#fff8e1;border-left:4px solid #f59e0b;
              border-radius:6px;font-size:13px;line-height:1.7">
    <p style="margin:0 0 8px;font-weight:bold;color:#b45309">
      ⚠ ライセンスキーの保管についてのお願い
    </p>
    <ul style="margin:0;padding-left:18px;color:#444">
      <li>ライセンスキーは大切に保管してください（端末移行・再認証時に必要になります）</li>
      <li>紛失時の再発行・再通知は原則として対応いたしかねます</li>
      <li>本メールはご購入時のメールアドレス宛にお送りしています。そのまま保存しておくことを強くおすすめします</li>
    </ul>
  </div>

  <p>アプリ内で「ヘルプ」→「ライセンス」→「ライセンスキーを入力」から上記キーを入力してください。</p>

  <hr style="margin:24px 0;border:none;border-top:1px solid #ddd">

  ${infoBlock}

  <p style="font-size:12px;color:#999;margin-top:16px;line-height:1.6">
    このメールは自動送信されています。ご不明な点は参照番号を添えてサポート
    (<a href="mailto:${SUPPORT_EMAIL}" style="color:#666">${SUPPORT_EMAIL}</a>)
    までご連絡ください。
  </p>
</body>
</html>`;

  const res = await fetch("https://api.resend.com/emails", {
    method: "POST",
    headers: { "Content-Type": "application/json", "Authorization": `Bearer ${apiKey}` },
    body: JSON.stringify({ from, to: [toEmail], subject: cfg.subject, html }),
  });

  if (!res.ok) {
    const errBody = await res.text();
    throw new Error(`Resend API error ${res.status}: ${errBody}`);
  }
}

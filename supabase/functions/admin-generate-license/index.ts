// Supabase Edge Function: admin-generate-license
// バックオフィス用・手動ライセンス発行（法人・銀行振込対応）
//
// 認証: x-admin-api-key ヘッダー（ADMIN_API_KEY シークレット）
// デプロイ: supabase functions deploy admin-generate-license --no-verify-jwt
//
// 処理フロー:
//   1. ADMIN_API_KEY で認証
//   2. count 件分のライセンスキーをサーバー側で HMAC 生成
//   3. licenses テーブルに一括 INSERT
//   4. 購入者に全キーを記載したメール（CC: support）を Resend 経由で送信
//   5. licenses 配列（licenseId + 表示用キー）を返す

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "@supabase/supabase-js";

const SUPABASE_URL              = Deno.env.get("SUPABASE_URL") ?? "";
const SUPABASE_SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";
const LICENSE_SECRET_KEY        = Deno.env.get("LICENSE_SECRET_KEY") ?? "";
const ADMIN_API_KEY             = Deno.env.get("ADMIN_API_KEY") ?? "";
const RESEND_API_KEY            = Deno.env.get("RESEND_API_KEY") ?? "";
const SUPPORT_EMAIL             = "support@office-goplan.com";

const APP_NAMES: Record<string, string> = {
  PDFH: "pdfHandler",
  ZIPS: "ZipSearch",
  PICT: "PictComp",
};

const MAX_COUNT   = 20; // 一括発行の上限
const JSON_HEADERS = { "Content-Type": "application/json" };

// ─── ライセンスキー生成（LicenseKeyHelper.cs と同アルゴリズム）──────────────
async function generateLicenseKey(
  secretKey: string,
  formCode: string,
): Promise<{ normalized: string; display: string }> {
  const serialBytes = crypto.getRandomValues(new Uint8Array(14));
  const serial = Array.from(serialBytes)
    .map((b) => b.toString(16).padStart(2, "0").toUpperCase())
    .join("");

  const message   = `PDFH:${formCode}:${serial}`;
  const cryptoKey = await crypto.subtle.importKey(
    "raw",
    new TextEncoder().encode(secretKey),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
  const sig     = await crypto.subtle.sign("HMAC", cryptoKey, new TextEncoder().encode(message));
  const hmacStr = Array.from(new Uint8Array(sig))
    .map((b) => b.toString(16).padStart(2, "0").toUpperCase())
    .join("");

  const normalized    = `PDFH-${formCode}-${serial}-${hmacStr}`;
  const serialDisplay = (serial.match(/.{1,4}/g) ?? []).join("-");
  const display       = `PDFH-${formCode}-${serialDisplay}-${hmacStr}`;

  return { normalized, display };
}

// ─── メール本文（複数キー対応）────────────────────────────────────────────────
function buildKeyBlock(key: string): string {
  return `<div style="background:#f8f8f8;border-left:4px solid #4CAF50;padding:12px 16px;
                      margin:8px 0;font-family:monospace;font-size:12px;
                      word-break:break-all;line-height:1.8">${key}</div>`;
}

function buildCustomerEmail(
  toName: string,
  appName: string,
  displayKeys: string[],
  invoiceNumber: string | null,
  customerCompany: string | null,
): string {
  const count       = displayKeys.length;
  const countLabel  = count === 1 ? "ライセンスキー" : `ライセンスキー一覧（${count} 件）`;
  const invoiceLine = invoiceNumber
    ? `<p><strong>請求書番号:</strong> ${invoiceNumber}</p>`
    : "";
  const keyBlocks = count === 1
    ? buildKeyBlock(displayKeys[0])
    : displayKeys
        .map((k, i) => `<p style="margin:12px 0 4px"><strong>キー ${i + 1}:</strong></p>${buildKeyBlock(k)}`)
        .join("");
  const companyLine = customerCompany
    ? `<p style="margin:0 0 2px">${customerCompany}</p>`
    : "";

  return `<!DOCTYPE html>
<html lang="ja">
<body style="font-family:sans-serif;max-width:600px;margin:0 auto;padding:24px;color:#333">
  ${companyLine}<p style="margin:0 0 16px">${toName} 様</p>
  <p>この度は <strong>${appName}</strong> をご購入いただき、誠にありがとうございます。<br>
  以下の${countLabel}をお送りいたします。</p>

  ${keyBlocks}

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

  ${invoiceLine}

  <h3 style="font-size:15px">アクティベーション手順</h3>
  <ol>
    <li>${appName} を起動します</li>
    <li>メニュー → <strong>ライセンス認証</strong> を開きます</li>
    <li>各 PC でライセンスキーを入力して「認証」をクリックします</li>
  </ol>

  <p>ご不明な点がございましたら、このメールにご返信いただくかサポートまでご連絡ください。</p>

  <p style="color:#bbb;font-family:monospace;margin:32px 0 8px">━━━━━━━━━━━━━━━━━━━━━━━━━━</p>
  <p style="margin:8px 0">
    <img src="https://6EFB0D.github.io/office-goplan/assets/logo/logo-a.jpg"
         alt="Office Go Plan" width="120"
         style="display:block;margin-bottom:8px">
  </p>
  <p style="color:#555;font-size:12px;line-height:2;margin:0">
    〒106-0032<br>
    東京都港区六本木2-1-19&nbsp;&nbsp;S-Building 3F<br>
    Mail: <a href="mailto:${SUPPORT_EMAIL}" style="color:#555">${SUPPORT_EMAIL}</a><br>
    <a href="https://6EFB0D.github.io/office-goplan/" style="color:#555">https://6EFB0D.github.io/office-goplan/</a>
  </p>
  <p style="color:#bbb;font-family:monospace;margin:8px 0 0">━━━━━━━━━━━━━━━━━━━━━━━━━━</p>
</body>
</html>`;
}

// ─── メイン ─────────────────────────────────────────────────────────────────
serve(async (req) => {
  if (req.method !== "POST") {
    return new Response(
      JSON.stringify({ error: "Method not allowed" }),
      { status: 405, headers: JSON_HEADERS },
    );
  }

  // 認証
  const incoming = req.headers.get("x-admin-api-key") ?? "";
  if (!ADMIN_API_KEY || incoming !== ADMIN_API_KEY) {
    return new Response(
      JSON.stringify({ error: "Unauthorized" }),
      { status: 401, headers: JSON_HEADERS },
    );
  }

  try {
    const body = await req.json();
    const {
      appId              = "PDFH",
      formCode           = "P101",
      userEmail,
      customerCompany    = null,
      customerContact    = null,
      invoiceNumber      = null,
      paymentConfirmedAt = null,
      isActive           = true,
      notes              = null,
    } = body;

    // 発行数（1〜MAX_COUNT、未指定は 1）
    const rawCount = parseInt(String(body.count ?? 1), 10);
    const count    = isNaN(rawCount) ? 1 : Math.min(Math.max(1, rawCount), MAX_COUNT);

    // バリデーション
    if (!userEmail || !String(userEmail).includes("@")) {
      return new Response(
        JSON.stringify({ error: "userEmail は必須です" }),
        { status: 400, headers: JSON_HEADERS },
      );
    }
    if (!LICENSE_SECRET_KEY) {
      console.error("LICENSE_SECRET_KEY が未設定");
      return new Response(
        JSON.stringify({ error: "Internal server error" }),
        { status: 500, headers: JSON_HEADERS },
      );
    }

    // 1. count 件分のライセンスキーを生成
    const generatedKeys: Array<{ normalized: string; display: string }> = [];
    for (let i = 0; i < count; i++) {
      generatedKeys.push(await generateLicenseKey(LICENSE_SECRET_KEY, formCode));
    }

    // 2. Supabase に一括 INSERT
    const supabase = createClient(SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY);
    const records  = generatedKeys.map((k) => ({
      license_key:          k.normalized,
      plan:                 "purchased",
      app_id:               appId,
      user_email:           userEmail,
      is_active:            isActive,
      activation_count:     0,
      payment_type:         "manual",
      customer_company:     customerCompany,
      customer_contact:     customerContact,
      invoice_number:       invoiceNumber,
      payment_confirmed_at: paymentConfirmedAt,
      notes:                notes,
    }));

    const { data: inserted, error: insertError } = await supabase
      .from("licenses")
      .insert(records)
      .select("id");

    if (insertError || !inserted || inserted.length !== count) {
      console.error("INSERT error:", insertError);
      return new Response(
        JSON.stringify({ error: "Internal server error" }),
        { status: 500, headers: JSON_HEADERS },
      );
    }

    // 3. レスポンス用データを組立て
    const licenses = inserted.map((row: { id: string }, i: number) => ({
      licenseId:         row.id,
      licenseKeyDisplay: generatedKeys[i].display,
    }));

    const appName  = APP_NAMES[appId] ?? appId;
    const toName   = customerContact ?? userEmail;
    const allKeys  = generatedKeys.map((k) => k.display);

    // 4. メール送信（全キーを1通に集約、CC: support）
    let emailSent = false;
    if (RESEND_API_KEY) {
      try {
        const resp = await fetch("https://api.resend.com/emails", {
          method: "POST",
          headers: {
            "Authorization": `Bearer ${RESEND_API_KEY}`,
            "Content-Type":  "application/json",
          },
          body: JSON.stringify({
            from:    `Office Go Plan <${SUPPORT_EMAIL}>`,
            to:      [userEmail],
            cc:      [SUPPORT_EMAIL],
            subject: `【Office Go Plan】${appName} ライセンスキーのご案内（${count} 件）`,
            html:    buildCustomerEmail(toName, appName, allKeys, invoiceNumber, customerCompany),
          }),
        });
        if (!resp.ok) {
          console.error("Resend error:", await resp.text());
        } else {
          emailSent = true;
        }
      } catch (e) {
        console.error("Email send error:", e);
      }
    }

    return new Response(
      JSON.stringify({ success: true, count, licenses, emailSent }),
      { status: 200, headers: JSON_HEADERS },
    );

  } catch (error) {
    console.error("admin-generate-license:", error);
    return new Response(
      JSON.stringify({ error: "Internal server error" }),
      { status: 500, headers: JSON_HEADERS },
    );
  }
});

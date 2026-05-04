/**
 * コンパクトライセンスキー（記号として32文字 + 区切りハイフン）
 * - データ32文字: [app 4][form 4][Crockford Base32 × 24]
 * - Supabase 用正準形: 全体を4文字ごとにハイフン（計 32+7=39 文字）
 */

const CROCKFORD = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

/** ハイフンを除いた正準32文字 */
export const COMPACT_PLAIN_RE =
  /^(PDFH|ZIPS|PICT)(P[12]\d{2})([0-9A-HJKMNP-TV-Z]{24})$/;

export function stripSeparators(key: string): string {
  return key.replace(/[\s-]/g, "");
}

/** 32文字（平文）→ Supabase / メール / UI 用の区切り形式 */
export function formatStorageFromPlain32(plain32: string): string {
  if (plain32.length !== 32) return plain32;
  const parts: string[] = [];
  for (let i = 0; i < 32; i += 4) {
    parts.push(plain32.slice(i, i + 4));
  }
  return parts.join("-");
}

export function encodeCrockford15(bytes: Uint8Array): string {
  if (bytes.length !== 15) throw new Error("encodeCrockford15: need 15 bytes");
  let buffer = 0;
  let bits = 0;
  let out = "";
  for (let i = 0; i < 15; i++) {
    buffer = (buffer << 8) | bytes[i]!;
    bits += 8;
    while (bits >= 5) {
      bits -= 5;
      const idx = (buffer >> bits) & 31;
      out += CROCKFORD[idx]!;
    }
  }
  if (bits > 0) {
    const idx = (buffer << (5 - bits)) & 31;
    out += CROCKFORD[idx]!;
  }
  return out.slice(0, 24);
}

export function decodeCrockford24(s: string): Uint8Array | null {
  if (s.length !== 24) return null;
  const rev = new Map<string, number>();
  for (let i = 0; i < CROCKFORD.length; i++) {
    rev.set(CROCKFORD[i]!, i);
  }
  let buffer = 0;
  let bits = 0;
  const out: number[] = [];
  for (const ch of s) {
    const idx = rev.get(ch);
    if (idx === undefined) return null;
    buffer = (buffer << 5) | idx;
    bits += 5;
    while (bits >= 8) {
      bits -= 8;
      out.push((buffer >> bits) & 255);
    }
  }
  if (out.length !== 15) return null;
  return Uint8Array.from(out);
}

/** 平文32へ正規化（検証: Crockford 再符号化） */
export function toCanonicalPlain32(input: string): string | null {
  const stripped = stripSeparators(input).toUpperCase();
  if (stripped.length !== 32 || !COMPACT_PLAIN_RE.test(stripped)) return null;
  const dec = decodeCrockford24(stripped.slice(8));
  if (!dec) return null;
  return stripped.slice(0, 8) + encodeCrockford15(dec);
}

/** DB・照合用の区切り付き正準形 */
export function normalizeCompactToStorage(input: string): string | null {
  const plain = toCanonicalPlain32(input);
  if (!plain) return null;
  return formatStorageFromPlain32(plain);
}

export async function generateCompactLicenseKey(
  secretKey: string | undefined,
  appId: string,
  formCode: string,
): Promise<{ plain32: string; storageKey: string }> {
  const prefix = appId.toUpperCase().slice(0, 4);
  if (!/^[A-Z0-9]{4}$/.test(prefix)) {
    throw new Error("appId must be 4 alphanumeric chars");
  }
  if (!/^P[12]\d{2}$/.test(formCode)) {
    throw new Error("invalid formCode");
  }

  const serial11 = crypto.getRandomValues(new Uint8Array(11));
  const hex11 = [...serial11]
    .map((b) => b.toString(16).padStart(2, "0"))
    .join("")
    .toUpperCase();
  const message = `${prefix}:${formCode}:${hex11}`;

  const mac4 = new Uint8Array(4);
  if (secretKey) {
    const cryptoKey = await crypto.subtle.importKey(
      "raw",
      new TextEncoder().encode(secretKey),
      { name: "HMAC", hash: "SHA-256" },
      false,
      ["sign"],
    );
    const sig = new Uint8Array(
      await crypto.subtle.sign(
        "HMAC",
        cryptoKey,
        new TextEncoder().encode(message),
      ),
    );
    mac4.set(sig.subarray(0, 4));
  } else {
    crypto.getRandomValues(mac4);
  }

  const payload = new Uint8Array(15);
  payload.set(serial11, 0);
  payload.set(mac4, 11);
  const plain32 = `${prefix}${formCode}${encodeCrockford15(payload)}`;
  return { plain32, storageKey: formatStorageFromPlain32(plain32) };
}

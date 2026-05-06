/**
 * Edge Function で licenses.license_key と突き合わせるとき用。
 * DB に「コンパクト正準・4文字区切り」以外のぶれ（ PREFIX-FORM-24 の3ブロック等）がある場合もヒットさせる。
 */
import {
  normalizeCompactToStorage,
  compactLookupKeyVariants,
} from "./compact-license-key.ts";
import { normalizeLicensePaste } from "./license-paste-normalize.ts";

/** DB 検索前のユーザー入力由来の「長形式／旧コンパクト」単一キーへ正規化 */
export function normalizeLicenseKeyForEdge(key: string): string | null {
  key = normalizeLicensePaste(key);
  if (!key) return null;

  const compact = normalizeCompactToStorage(key);
  if (compact) return compact;

  const trimmed = key.trim().toUpperCase();
  const parts = trimmed.split("-");
  if (parts.length < 3) return null;

  const prefix = parts[0];
  if (!prefix || !/^[A-Z0-9]+$/.test(prefix)) return null;

  const formCode = parts[1];
  if (!formCode || formCode.length !== 4) return null;

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

/** .in("license_key", …) で渡す候補（重複排除済み・空除去） */
export function licenseDbLookupKeys(raw: string): string[] {
  const sanitized = normalizeLicensePaste(raw);
  const norm = normalizeLicenseKeyForEdge(sanitized);
  const compactVars = compactLookupKeyVariants(sanitized);
  const trimmed = sanitized.trim();

  const uniq = new Set<string>([
    ...compactVars,
    ...(norm ? [norm] : []),
    ...(trimmed ? [trimmed] : []),
  ]);
  return [...uniq].filter((s) => s.length > 0);
}

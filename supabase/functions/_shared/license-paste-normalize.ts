/**
 * メール・Word 経由ペーストで混入しがちな不可視文字・異種ハイフンを正規化。
 * Crockford と DB 検索が ASCII 「-」前提のため、検索前に必ず通す。
 */
export function normalizeLicensePaste(raw: unknown): string {
  if (raw === undefined || raw === null) return "";
  let s = String(raw).normalize("NFKC");

  // ZWNBSP・ゼロ幅・BOM 等を除去（strip では読みやすく残すだけにする場合は削除のみ）
  s = s.replace(/[\uFEFF\u200B-\u200D\u2060]/g, "");

  // ノーブレークスペース等 → 半角スペース（後続 trim / 処理用）
  s = s.replace(/[\u00A0\u202F]/g, " ");

  // 各種ハイフン類 → ASCII '-'
  s = s.replace(/[\u2010\u2011\u2012\u2013\u2014\u2212\uFF0D]/g, "-");

  return s.trim();
}

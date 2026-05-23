// メールアドレスのマスク（ログ出力用）
// QM 2.2 §1.1 / §1.3 — ログに PII を載せない

/**
 * メールアドレスの先頭1文字と @以降だけを残してマスクする。
 * 例: "user@example.com" → "u***@example.com"
 * 不正な形式の場合は "***" を返す。
 */
export function maskEmail(email: string): string {
  const at = email.indexOf("@");
  return at > 0 ? `${email.slice(0, 1)}***${email.slice(at)}` : "***";
}

// 製品単位でのライセンス利用を Edge Function で強制する（DB の app_id とキー先頭の一致確認）

/** クライアント送信の製品コード（PDFH / ZIPS / PICT など）を正規化 */
export function normalizeClientAppId(raw: unknown): string | null {
  if (raw === undefined || raw === null) return null;
  const t = String(raw).trim().toUpperCase().replace(/[^A-Z0-9]/g, "");
  if (t.length < 4) return null;
  return t.slice(0, 4);
}

/** DB 保管形式のライセンスキー先頭セグメント（4 英数字）から製品コードを取得 */
export function appIdPrefixFromLicenseKeyStorage(normalizedKey: string): string | null {
  const part = (normalizedKey.trim().split("-")[0] ?? "").toUpperCase();
  if (!/^[A-Z0-9]{4}$/.test(part)) return null;
  return part;
}

export type LicenseAppGuardResult =
  | { ok: true }
  | { ok: false; userMessage: string };

/**
 * licenses 行とクライアントの製品コードが一致するか。
 * clientAppId 未送信は不合格（後方互換を切る）。
 */
export function assertLicenseBelongsToClientApp(
  license: { app_id?: string | null; license_key: string },
  clientAppIdRaw: unknown,
): LicenseAppGuardResult {
  const client = normalizeClientAppId(clientAppIdRaw);
  if (!client) {
    return {
      ok: false,
      userMessage:
        "アプリが古いためライセンスを検証できません。最新版に更新してください。（clientAppId）",
    };
  }

  const fromKey = appIdPrefixFromLicenseKeyStorage(license.license_key);
  const fromRow = license.app_id != null && String(license.app_id).trim() !== ""
    ? normalizeClientAppId(license.app_id)
    : null;

  if (fromRow && fromKey && fromRow !== fromKey) {
    return {
      ok: false,
      userMessage: "ライセンスデータの参照に不整合があります。サポートにお問い合わせください。",
    };
  }

  const expected = fromRow ?? fromKey;
  if (!expected) {
    return {
      ok: false,
      userMessage: "このライセンスはこの製品では使用できません。",
    };
  }

  if (expected !== client) {
    return {
      ok: false,
      userMessage: "このライセンスは別の製品用です。お手元のアプリ用のキーを入力してください。",
    };
  }

  return { ok: true };
}

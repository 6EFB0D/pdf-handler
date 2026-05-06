// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2026 Office Go Plan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PdfHandler.Infrastructure.Helpers;

/// <summary>
/// アプリケーション全体で使用するエラーコード定数。
/// ユーザーに表示する際は内部詳細を隠し、このコードのみ提示する。
/// ログにはコードと完全な例外情報を併記する。
///
/// 体系: PDFH-XXXX
///   1xxx = ライセンス・アクティベーション
///   2xxx = ネットワーク・API通信
///   3xxx = ファイル操作
///   4xxx = PDF処理
///   9xxx = その他・予期せぬエラー
/// </summary>
public static class ErrorCodes
{
    // ─────────────────────────────────────────
    // 1xxx: ライセンス・アクティベーション
    // ─────────────────────────────────────────

    /// <summary>ライセンス情報の読み込み失敗</summary>
    public const string LicenseLoadFailed       = "PDFH-1001";

    /// <summary>ライセンスキーの検証失敗</summary>
    public const string LicenseVerifyFailed     = "PDFH-1002";

    /// <summary>アクティベーション失敗</summary>
    public const string ActivationFailed        = "PDFH-1003";

    /// <summary>デアクティベーション失敗</summary>
    public const string DeactivationFailed      = "PDFH-1004";

    /// <summary>デバイス上限超過</summary>
    public const string DeviceLimitExceeded     = "PDFH-1005";

    /// <summary>購入フロー開始失敗</summary>
    public const string PurchaseStartFailed     = "PDFH-1006";

    /// <summary>ライセンス一覧取得失敗</summary>
    public const string LicenseListFailed       = "PDFH-1007";

    // ─────────────────────────────────────────
    // 2xxx: ネットワーク・API通信
    // ─────────────────────────────────────────

    /// <summary>サーバーへの接続失敗</summary>
    public const string NetworkConnectionFailed = "PDFH-2001";

    /// <summary>APIリクエストのタイムアウト</summary>
    public const string ApiTimeout              = "PDFH-2002";

    /// <summary>APIレスポンスの解析失敗</summary>
    public const string ApiResponseParseFailed  = "PDFH-2003";

    /// <summary>アップデート確認失敗</summary>
    public const string UpdateCheckFailed       = "PDFH-2004";

    /// <summary>URLオープン失敗</summary>
    public const string UrlOpenFailed           = "PDFH-2005";

    // ─────────────────────────────────────────
    // 3xxx: ファイル操作
    // ─────────────────────────────────────────

    /// <summary>ファイルを開けない</summary>
    public const string FileOpenFailed          = "PDFH-3001";

    /// <summary>ファイルの読み込み失敗</summary>
    public const string FileReadFailed          = "PDFH-3002";

    /// <summary>ファイルの書き込み失敗</summary>
    public const string FileWriteFailed         = "PDFH-3003";

    /// <summary>ファイルのコピー・移動失敗</summary>
    public const string FileCopyFailed          = "PDFH-3004";

    /// <summary>ファイルの削除失敗</summary>
    public const string FileDeleteFailed        = "PDFH-3005";

    /// <summary>ファイル名変更失敗</summary>
    public const string FileRenameFailed        = "PDFH-3006";

    /// <summary>取扱説明書・法的文書の表示失敗</summary>
    public const string DocumentOpenFailed      = "PDFH-3007";

    // ─────────────────────────────────────────
    // 4xxx: PDF処理
    // ─────────────────────────────────────────

    /// <summary>PDFの読み込み失敗</summary>
    public const string PdfLoadFailed           = "PDFH-4001";

    /// <summary>PDF結合失敗</summary>
    public const string PdfMergeFailed          = "PDFH-4002";

    /// <summary>PDF分割失敗</summary>
    public const string PdfSplitFailed          = "PDFH-4003";

    /// <summary>ヘッダ・フッター適用失敗</summary>
    public const string PdfHeaderFooterFailed   = "PDFH-4004";

    /// <summary>PDFプレビュー生成失敗</summary>
    public const string PdfPreviewFailed        = "PDFH-4005";

    // ─────────────────────────────────────────
    // 9xxx: その他・予期せぬエラー
    // ─────────────────────────────────────────

    /// <summary>予期しない例外</summary>
    public const string Unexpected              = "PDFH-9001";

    /// <summary>設定の読み書き失敗</summary>
    public const string SettingsFailed          = "PDFH-9002";

    /// <summary>UIの初期化失敗</summary>
    public const string UiInitFailed            = "PDFH-9003";

    // ─────────────────────────────────────────
    // ユーティリティ
    // ─────────────────────────────────────────

    /// <summary>
    /// ユーザーに表示するエラーメッセージを生成する。
    /// 内部詳細は含まず、エラーコードのみを提示する。
    /// </summary>
    public static string UserMessage(string code, string? context = null)
    {
        var ctx = string.IsNullOrEmpty(context) ? "" : $"{context}\n\n";
        return $"{ctx}エラーが発生しました。\nエラーコード: {code}\n\n" +
               $"サポートにお問い合わせの際は、このコードをお伝えください。\n" +
               $"（お問い合わせ: ヘルプ → お問い合わせフォーム）";
    }
}

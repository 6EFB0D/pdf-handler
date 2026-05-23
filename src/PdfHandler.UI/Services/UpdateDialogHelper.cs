// PDFハンドラ (PDF Handler)
// Copyright (c) 2025-2026 Goplan. All rights reserved.

using System;
using System.Diagnostics;
using System.Windows;
using PdfHandler.Infrastructure.Helpers;
using PdfHandler.UI.Models;

namespace PdfHandler.UI.Services;

/// <summary>
/// 更新確認ダイアログの文言・表示（About と起動時通知で共通）。
/// </summary>
public static class UpdateDialogHelper
{
    public static bool IsMajorUpgrade(UpdateInfo updateInfo) =>
        ReleaseVersionHelper.GetMajorVersion(updateInfo.LatestVersion)
        > ReleaseVersionHelper.GetMajorVersion(updateInfo.CurrentVersion);

    public static (string Title, string Message, MessageBoxImage Icon) GetUpdateAvailableContent(UpdateInfo updateInfo)
    {
        var currentMajor = ReleaseVersionHelper.GetMajorVersion(updateInfo.CurrentVersion);
        var latestMajor = ReleaseVersionHelper.GetMajorVersion(updateInfo.LatestVersion);

        if (IsMajorUpgrade(updateInfo))
        {
            var message =
                $"🆕 新しいメジャーバージョン v{updateInfo.LatestVersion} が公開されています\n\n" +
                $"現在のバージョン: v{updateInfo.CurrentVersion}\n" +
                $"最新バージョン:   v{updateInfo.LatestVersion}\n\n" +
                $"⚠️ ご注意: v{latestMajor}.x.x は現在お持ちのライセンス（v{currentMajor}.x.x 対象）では\n" +
                $"ご利用いただけません。新しいライセンスのご購入が必要です。\n\n" +
                $"（利用規約 第8条 / 詳細はリリースページをご確認ください）\n\n" +
                $"リリースページを開きますか？";
            return ("メジャーバージョンアップのお知らせ", message, MessageBoxImage.Warning);
        }

        var freeMessage =
            $"🆕 新しいバージョンが利用可能です\n\n" +
            $"現在のバージョン: v{updateInfo.CurrentVersion}\n" +
            $"最新バージョン:   v{updateInfo.LatestVersion}\n\n" +
            $"✅ 現在のライセンス（v{currentMajor}.x.x 対象）でそのままご利用いただけます。\n\n" +
            $"リリース日: {updateInfo.FormattedReleaseDate}\n" +
            $"ファイルサイズ: {updateInfo.FormattedSize}\n\n" +
            $"ダウンロードページを開きますか？";
        return ("アップデート", freeMessage, MessageBoxImage.Information);
    }

    public static MessageBoxResult ShowUpdateAvailableDialog(UpdateInfo updateInfo, Window? owner = null)
    {
        var (title, message, icon) = GetUpdateAvailableContent(updateInfo);
        if (owner != null)
            return MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, icon);

        return MessageBox.Show(message, title, MessageBoxButton.YesNo, icon);
    }

    public static void ShowLatestVersionDialog(UpdateInfo updateInfo, Window? owner = null)
    {
        var message =
            $"✅ 最新版をご利用中です\n\n" +
            $"現在のバージョン: {updateInfo.CurrentVersion}\n" +
            $"最新バージョン: {updateInfo.LatestVersion}\n\n" +
            $"最終確認: {DateTime.Now:yyyy年MM月dd日 HH:mm}";

        if (owner != null)
            MessageBox.Show(owner, message, "更新確認", MessageBoxButton.OK, MessageBoxImage.Information);
        else
            MessageBox.Show(message, "更新確認", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static void ShowUpdateErrorDialog(UpdateInfo updateInfo, Window? owner = null)
    {
        var message =
            $"⚠️ 更新情報を取得できませんでした\n\n" +
            $"{updateInfo.ErrorMessage}\n\n" +
            $"手動で確認しますか？";

        MessageBoxResult result;
        if (owner != null)
            result = MessageBox.Show(owner, message, "更新確認エラー", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        else
            result = MessageBox.Show(message, "更新確認エラー", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
            OpenUrl("https://github.com/6EFB0D/pdf-handler/releases");
    }

    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(ErrorCodes.UrlOpenFailed, $"URLオープン失敗: {url}", ex);
            MessageBox.Show(
                ErrorCodes.UserMessage(ErrorCodes.UrlOpenFailed, "URLを開けませんでした。"),
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}

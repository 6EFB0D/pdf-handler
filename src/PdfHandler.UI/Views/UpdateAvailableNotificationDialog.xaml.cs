// PDFハンドラ (PDF Handler)
// Copyright (c) 2025-2026 Goplan. All rights reserved.

using System.Windows;
using PdfHandler.UI.Models;
using PdfHandler.UI.Services;

namespace PdfHandler.UI.Views;

public partial class UpdateAvailableNotificationDialog : Window
{
    private readonly UpdateInfo _updateInfo;

    public bool SuppressFutureNotifications { get; private set; }

    public UpdateAvailableNotificationDialog(UpdateInfo updateInfo)
    {
        InitializeComponent();
        _updateInfo = updateInfo;

        var (title, message, _) = UpdateDialogHelper.GetUpdateAvailableContent(updateInfo);
        Title = title;
        MessageTextBlock.Text = message.Replace(
            "\n\nリリースページを開きますか？",
            "\n\n下のボタンからリリースページを開けます。").Replace(
            "\n\nダウンロードページを開きますか？",
            "\n\n下のボタンからダウンロードページを開けます。");
    }

    private void OpenRelease_Click(object sender, RoutedEventArgs e)
    {
        SuppressFutureNotifications = SuppressCheckBox.IsChecked == true;
        if (!string.IsNullOrWhiteSpace(_updateInfo.DownloadUrl))
            UpdateDialogHelper.OpenUrl(_updateInfo.DownloadUrl);
        DialogResult = true;
        Close();
    }

    private void Later_Click(object sender, RoutedEventArgs e)
    {
        SuppressFutureNotifications = SuppressCheckBox.IsChecked == true;
        DialogResult = false;
        Close();
    }
}

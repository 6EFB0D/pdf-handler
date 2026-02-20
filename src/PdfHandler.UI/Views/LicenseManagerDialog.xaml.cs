// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PdfHandler.Core.Interfaces;
using PdfHandler.Core.Models;

namespace PdfHandler.UI.Views;

public partial class LicenseManagerDialog : Window
{
    private readonly ILicenseService _licenseService;
    private LicenseActivationsResult? _currentResult;

    public LicenseManagerDialog()
    {
        InitializeComponent();

        var app = (App)Application.Current;
        _licenseService = app.GetService<ILicenseService>();

        Loaded += async (_, _) => await LoadActivationsAsync();
    }

    private async System.Threading.Tasks.Task LoadActivationsAsync()
    {
        DeviceListPanel.Children.Clear();
        DeviceCountText.Text = "登録デバイス: 読み込み中...";

        // ライセンス番号を表示
        var licenseKey = _licenseService.GetLicenseKey();
        if (!string.IsNullOrEmpty(licenseKey))
        {
            LicenseKeyText.Text = licenseKey;
            LicenseKeyBorder.Visibility = Visibility.Visible;
        }
        else
        {
            LicenseKeyBorder.Visibility = Visibility.Collapsed;
        }

        _currentResult = await _licenseService.GetActivationsAsync();
        if (_currentResult == null)
        {
            DeviceCountText.Text = "登録デバイス: 読み込みに失敗しました（ネットワーク接続を確認してください）";
            DeviceCountText.Foreground = Brushes.DarkRed;
            return;
        }

        DeviceCountText.Text = $"登録デバイス: {_currentResult.DeviceCount}/{_currentResult.DeviceLimit}";
        DeviceCountText.Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85));

        foreach (var activation in _currentResult.Activations)
        {
            var card = CreateDeviceCard(activation);
            DeviceListPanel.Children.Add(card);
        }
    }

    private Border CreateDeviceCard(DeviceActivation activation)
    {
        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var mainStack = new StackPanel();

        // デバイス名＋「このPC」表示
        var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
        var nameText = new TextBlock
        {
            Text = activation.DisplayName,
            FontWeight = FontWeights.SemiBold,
            FontSize = 14,
            Foreground = Brushes.Black,
            VerticalAlignment = VerticalAlignment.Center
        };
        nameRow.Children.Add(nameText);

        if (activation.IsCurrentDevice)
        {
            var currentBadge = new TextBlock
            {
                Text = " （このPC・現在使用中）",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 102, 204)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            nameRow.Children.Add(currentBadge);
        }
        mainStack.Children.Add(nameRow);

        // 日付
        var dateText = new TextBlock
        {
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
            Margin = new Thickness(0, 4, 0, 0)
        };
        var actDate = activation.ActivationDate?.ToString("yyyy/MM/dd") ?? "-";
        var lastDate = activation.LastVerificationDate?.ToString("yyyy/MM/dd") ?? "-";
        dateText.Text = $"アクティベート: {actDate}  最終利用: {lastDate}";
        mainStack.Children.Add(dateText);

        // ボタン
        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var editButton = new Button
        {
            Content = "名前を編集",
            Width = 90,
            Height = 28,
            Margin = new Thickness(0, 0, 8, 0),
            FontSize = 12
        };
        editButton.Click += async (_, _) => await EditDeviceNameAsync(activation, mainStack, nameText);

        var deactivateButton = new Button
        {
            Content = "解除",
            Width = 70,
            Height = 28,
            FontSize = 12
        };
        deactivateButton.Click += async (_, _) => await DeactivateDeviceAsync(activation);

        buttonRow.Children.Add(editButton);
        buttonRow.Children.Add(deactivateButton);
        mainStack.Children.Add(buttonRow);

        border.Child = mainStack;
        border.Tag = activation;
        return border;
    }

    private async System.Threading.Tasks.Task EditDeviceNameAsync(DeviceActivation activation, Panel parent, TextBlock nameText)
    {
        var dialog = new EditDeviceNameDialog(activation.DisplayName)
        {
            Owner = this
        };
        if (dialog.ShowDialog() != true)
            return;

        var newName = dialog.DisplayName;
        if (string.IsNullOrWhiteSpace(newName))
        {
            MessageBox.Show("表示名を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var success = await _licenseService.UpdateDeviceDisplayNameAsync(activation.Id, newName);
        if (success)
        {
            nameText.Text = newName;
            MessageBox.Show("表示名を更新しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("表示名の更新に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async System.Threading.Tasks.Task DeactivateDeviceAsync(DeviceActivation activation)
    {
        var deviceLabel = activation.IsCurrentDevice ? "このPC" : activation.DisplayName;
        var result = MessageBox.Show(
            $"「{deviceLabel}」のライセンスを解除しますか？\n\n解除すると、このデバイスでライセンスを使用できなくなります。",
            "確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        var success = await _licenseService.DeactivateDeviceAsync(activation.Id);
        if (success)
        {
            if (activation.IsCurrentDevice)
            {
                // このPCを解除した場合、ローカルライセンスをクリア
                var license = _licenseService.GetLicenseInfo();
                license.Plan = LicensePlan.Trial;
                license.LicenseKey = null;
                license.ActivationDate = null;
                await _licenseService.SaveLicenseAsync(license);
                MessageBox.Show("このPCのライセンスを解除しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
                return;
            }
            await LoadActivationsAsync();
            MessageBox.Show("デバイスのライセンスを解除しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("解除に失敗しました。ネットワーク接続を確認してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyLicenseKey_Click(object sender, RoutedEventArgs e)
    {
        var key = _licenseService.GetLicenseKey();
        if (string.IsNullOrEmpty(key))
            return;
        try
        {
            Clipboard.SetText(key);
            MessageBox.Show("ライセンス番号をクリップボードにコピーしました。", "コピー完了",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"コピーに失敗しました。\n\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadActivationsAsync();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

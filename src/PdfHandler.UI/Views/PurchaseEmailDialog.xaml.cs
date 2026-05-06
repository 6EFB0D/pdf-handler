// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.

using System;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;

namespace PdfHandler.UI.Views;

public partial class PurchaseEmailDialog : Window
{
    /// <summary>確定したメール（ShowDialog が true のときのみ有効）</summary>
    public string CustomerEmail { get; private set; } = "";

    public PurchaseEmailDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => EmailTextBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var a = (EmailTextBox.Text ?? "").Trim();
        var b = (ConfirmEmailTextBox.Text ?? "").Trim();

        if (string.IsNullOrEmpty(a))
        {
            MessageBox.Show("メールアドレスを入力してください。", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _ = new MailAddress(a);
        }
        catch (FormatException)
        {
            MessageBox.Show("メールアドレスの形式が正しくないようです。", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!string.Equals(a, b, StringComparison.Ordinal))
        {
            MessageBox.Show("2つのメールアドレスが一致しません。\nもう一度入力してください。", this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CustomerEmail = a;
        DialogResult = true;
    }
}

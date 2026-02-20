// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows;

namespace PdfHandler.UI.Views;

public partial class EditDeviceNameDialog : Window
{
    public string DisplayName => DisplayNameTextBox.Text.Trim();

    public EditDeviceNameDialog(string currentName)
    {
        InitializeComponent();
        DisplayNameTextBox.Text = currentName ?? "";
        DisplayNameTextBox.SelectAll();
        DisplayNameTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

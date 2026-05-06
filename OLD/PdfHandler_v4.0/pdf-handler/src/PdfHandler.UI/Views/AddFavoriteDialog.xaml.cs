// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;
using System.Windows;

namespace PdfHandler.UI.Views;

public partial class AddFavoriteDialog : Window
{
    public string FavoriteName { get; private set; } = string.Empty;
    public string FavoritePath { get; private set; } = string.Empty;

    public AddFavoriteDialog(string folderPath)
    {
        InitializeComponent();

        FavoritePath = folderPath;
        PathText.Text = folderPath;
        
        // デフォルトの表示名（フォルダ名）
        NameTextBox.Text = Path.GetFileName(folderPath);
        NameTextBox.Focus();
        NameTextBox.SelectAll();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("表示名を入力してください。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            NameTextBox.Focus();
            return;
        }

        FavoriteName = name;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

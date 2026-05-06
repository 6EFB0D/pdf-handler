// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using PdfHandler.Core.Interfaces;
using PdfHandler.Core.Models;

namespace PdfHandler.UI.Views;

/// <summary>
/// BinderManagerDialog.xaml の相互作用ロジック
/// </summary>
public partial class BinderManagerDialog : Window
{
    private readonly IBinderService _binderService;
    private readonly ObservableCollection<Binder> _binders = new();
    private Binder? _selectedBinder;

    public BinderManagerDialog()
    {
        InitializeComponent();
        
        // DIコンテナから取得
        var app = (App)Application.Current;
        _binderService = app.GetService<IBinderService>();

        BinderListBox.ItemsSource = _binders;
        LoadBinders();
    }

    private async void LoadBinders()
    {
        var binders = await _binderService.GetAllBindersAsync();
        _binders.Clear();
        foreach (var binder in binders)
        {
            _binders.Add(binder);
        }
    }

    private async void BinderListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _selectedBinder = BinderListBox.SelectedItem as Binder;
        if (_selectedBinder != null)
        {
            BinderNameTextBlock.Text = _selectedBinder.Name;
            BinderDescriptionTextBlock.Text = _selectedBinder.Description ?? "説明なし";
            
            PdfFileListBox.ItemsSource = _selectedBinder.PdfFilePaths.Select(Path.GetFileName).ToList();
        }
        else
        {
            BinderNameTextBlock.Text = "";
            BinderDescriptionTextBlock.Text = "";
            PdfFileListBox.ItemsSource = null;
        }
    }

    private async void CreateBinder_Click(object sender, RoutedEventArgs e)
    {
        // 簡易的な名前入力ダイアログ
        var nameDialog = new Window
        {
            Title = "バインダー名を入力",
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize
        };

        var textBox = new System.Windows.Controls.TextBox
        {
            Text = "新しいバインダー",
            Margin = new Thickness(10),
            VerticalAlignment = VerticalAlignment.Top
        };

        var okButton = new System.Windows.Controls.Button
        {
            Content = "OK",
            Width = 75,
            Height = 25,
            Margin = new Thickness(5),
            IsDefault = true
        };

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "キャンセル",
            Width = 75,
            Height = 25,
            Margin = new Thickness(5),
            IsCancel = true
        };

        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(10)
        };
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        var grid = new System.Windows.Controls.Grid();
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.Children.Add(textBox);
        System.Windows.Controls.Grid.SetRow(buttonPanel, 1);
        grid.Children.Add(buttonPanel);

        nameDialog.Content = grid;

        bool? result = null;
        okButton.Click += (s, args) => { result = true; nameDialog.Close(); };
        cancelButton.Click += (s, args) => { result = false; nameDialog.Close(); };

        nameDialog.ShowDialog();

        if (result == true)
        {
            var binderName = textBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(binderName))
            {
                var binder = await _binderService.CreateBinderAsync(binderName);
                _binders.Insert(0, binder);
                BinderListBox.SelectedItem = binder;
            }
        }
    }

    private async void DeleteBinder_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedBinder == null)
        {
            MessageBox.Show("削除するバインダーを選択してください。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"バインダー '{_selectedBinder.Name}' を削除しますか？",
            "確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _binderService.DeleteBinderAsync(_selectedBinder.Id);
            _binders.Remove(_selectedBinder);
            _selectedBinder = null;
            BinderNameTextBlock.Text = "";
            BinderDescriptionTextBlock.Text = "";
            PdfFileListBox.ItemsSource = null;
        }
    }

    private async void AddPdfToBinder_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedBinder == null)
        {
            MessageBox.Show("PDFファイルを追加するバインダーを選択してください。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "PDFファイルを選択",
            Filter = "PDFファイル|*.pdf",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var filePath in dialog.FileNames)
            {
                await _binderService.AddPdfToBinderAsync(_selectedBinder.Id, filePath);
            }

            // バインダーを再読み込み
            var updatedBinder = await _binderService.GetBinderAsync(_selectedBinder.Id);
            if (updatedBinder != null)
            {
                _selectedBinder = updatedBinder;
                PdfFileListBox.ItemsSource = _selectedBinder.PdfFilePaths.Select(Path.GetFileName).ToList();
            }
        }
    }

    private async void MergeBinder_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedBinder == null)
        {
            MessageBox.Show("結合するバインダーを選択してください。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "結合したPDFファイルの保存先を選択",
            Filter = "PDFファイル|*.pdf",
            FileName = $"{_selectedBinder.Name}.pdf"
        };

        if (dialog.ShowDialog() == true)
        {
            var progress = new Progress<int>(percent =>
            {
                // 進捗表示（簡易実装）
            });

            var success = await _binderService.MergeBinderToPdfAsync(_selectedBinder.Id, dialog.FileName, progress);
            
            if (success)
            {
                MessageBox.Show($"バインダーをPDFファイルに結合しました。\n保存先: {dialog.FileName}", "完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("バインダーの結合に失敗しました。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}


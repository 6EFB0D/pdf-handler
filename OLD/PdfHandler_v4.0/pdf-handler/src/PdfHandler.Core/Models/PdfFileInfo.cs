// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;

namespace PdfHandler.Core.Models;

/// <summary>
/// PDFファイル情報を表すモデル
/// </summary>
public partial class PdfFileInfo : ObservableObject
{
    /// <summary>
    /// ファイルの完全パス
    /// </summary>
    [ObservableProperty]
    private string _filePath = string.Empty;

    /// <summary>
    /// ファイル名（拡張子含む）
    /// </summary>
    [ObservableProperty]
    private string _fileName = string.Empty;

    /// <summary>
    /// ファイルサイズ（バイト）
    /// </summary>
    [ObservableProperty]
    private long _fileSize;

    /// <summary>
    /// 最終更新日時
    /// </summary>
    [ObservableProperty]
    private DateTime _lastModified;

    /// <summary>
    /// ページ数
    /// </summary>
    [ObservableProperty]
    private int _pageCount;

    /// <summary>
    /// サムネイル画像データ（第1ページ）
    /// </summary>
    [ObservableProperty]
    private byte[]? _thumbnailData;

    /// <summary>
    /// 選択状態（複数選択用）
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// 編集モード
    /// </summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>
    /// 編集中のファイル名
    /// </summary>
    [ObservableProperty]
    private string _editingName = string.Empty;

    /// <summary>
    /// ファイルサイズを人間が読みやすい形式で取得
    /// </summary>
    public string FormattedFileSize
    {
        get
        {
            if (FileSize < 1024)
                return $"{FileSize} B";
            if (FileSize < 1024 * 1024)
                return $"{FileSize / 1024.0:F1} KB";
            if (FileSize < 1024 * 1024 * 1024)
                return $"{FileSize / (1024.0 * 1024.0):F1} MB";
            return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
    }
}

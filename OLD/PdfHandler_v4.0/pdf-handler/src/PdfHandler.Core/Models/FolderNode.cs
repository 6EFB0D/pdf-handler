// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;

namespace PdfHandler.Core.Models;

/// <summary>
/// フォルダツリーノードを表すモデル
/// </summary>
public partial class FolderNode : ObservableObject
{
    /// <summary>
    /// フォルダの完全パス
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// フォルダ名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 親フォルダノード
    /// </summary>
    public FolderNode? Parent { get; set; }

    /// <summary>
    /// 子フォルダノードのリスト
    /// </summary>
    public List<FolderNode> Children { get; set; } = new();

    /// <summary>
    /// 展開状態
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// 選択状態
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;
}

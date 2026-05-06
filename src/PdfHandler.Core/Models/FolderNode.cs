// PDFハンドラ (PDF Handler)
// Copyright (c) 2025-2026 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

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
    public ObservableCollection<FolderNode> Children { get; set; } = new();

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

    /// <summary>
    /// 子フォルダが読み込まれているかどうか（遅延読み込み用）
    /// </summary>
    public bool IsChildrenLoaded { get; set; } = false;

    /// <summary>
    /// タグ（お気に入りなど識別用）
    /// </summary>
    public string? Tag { get; set; }
}

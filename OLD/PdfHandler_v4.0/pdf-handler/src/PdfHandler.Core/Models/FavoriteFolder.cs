// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PdfHandler.Core.Models;

/// <summary>
/// お気に入りフォルダ
/// </summary>
public class FavoriteFolder
{
    /// <summary>
    /// 表示名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// フォルダパス
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 追加日時
    /// </summary>
    public DateTime AddedDate { get; set; } = DateTime.Now;
}

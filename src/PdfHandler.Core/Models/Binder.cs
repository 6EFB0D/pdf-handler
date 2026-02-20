// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PdfHandler.Core.Models;

/// <summary>
/// PDFバインダーモデル
/// </summary>
public class Binder
{
    /// <summary>
    /// バインダーID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// バインダー名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 作成日時
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新日時
    /// </summary>
    public DateTime UpdatedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// バインダーに含まれるPDFファイルのパスリスト
    /// </summary>
    public List<string> PdfFilePaths { get; set; } = new();

    /// <summary>
    /// バインダーの説明
    /// </summary>
    public string? Description { get; set; }
}







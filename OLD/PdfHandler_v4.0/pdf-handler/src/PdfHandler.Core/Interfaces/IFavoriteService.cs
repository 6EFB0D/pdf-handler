// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using PdfHandler.Core.Models;

namespace PdfHandler.Core.Interfaces;

/// <summary>
/// お気に入りフォルダ管理サービス
/// </summary>
public interface IFavoriteService
{
    /// <summary>
    /// お気に入りフォルダ一覧を取得
    /// </summary>
    Task<List<FavoriteFolder>> GetFavoritesAsync();

    /// <summary>
    /// お気に入りフォルダを追加
    /// </summary>
    Task<bool> AddFavoriteAsync(string name, string path);

    /// <summary>
    /// お気に入りフォルダを削除
    /// </summary>
    Task<bool> RemoveFavoriteAsync(string path);

    /// <summary>
    /// お気に入りフォルダ名を変更
    /// </summary>
    Task<bool> RenameFavoriteAsync(string path, string newName);

    /// <summary>
    /// お気に入りを保存
    /// </summary>
    Task SaveFavoritesAsync(List<FavoriteFolder> favorites);
}

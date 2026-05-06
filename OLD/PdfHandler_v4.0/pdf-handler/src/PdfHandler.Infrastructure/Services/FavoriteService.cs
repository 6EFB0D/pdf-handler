// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using PdfHandler.Core.Interfaces;
using PdfHandler.Core.Models;
using System.Text.Json;

namespace PdfHandler.Infrastructure.Services;

/// <summary>
/// お気に入りフォルダ管理サービスの実装
/// </summary>
public class FavoriteService : IFavoriteService
{
    private readonly string _favoritesFilePath;

    public FavoriteService()
    {
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PdfHandler");
        
        if (!Directory.Exists(appDataFolder))
            Directory.CreateDirectory(appDataFolder);

        _favoritesFilePath = Path.Combine(appDataFolder, "favorites.json");
    }

    public async Task<List<FavoriteFolder>> GetFavoritesAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(_favoritesFilePath))
                    return new List<FavoriteFolder>();

                var json = File.ReadAllText(_favoritesFilePath);
                return JsonSerializer.Deserialize<List<FavoriteFolder>>(json) ?? new List<FavoriteFolder>();
            }
            catch
            {
                return new List<FavoriteFolder>();
            }
        });
    }

    public async Task<bool> AddFavoriteAsync(string name, string path)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var favorites = await GetFavoritesAsync();

                // 既に存在する場合は追加しない
                if (favorites.Any(f => f.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    return false;

                favorites.Add(new FavoriteFolder
                {
                    Name = name,
                    Path = path,
                    AddedDate = DateTime.Now
                });

                await SaveFavoritesAsync(favorites);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<bool> RemoveFavoriteAsync(string path)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var favorites = await GetFavoritesAsync();
                var removed = favorites.RemoveAll(f => f.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

                if (removed > 0)
                {
                    await SaveFavoritesAsync(favorites);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<bool> RenameFavoriteAsync(string path, string newName)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var favorites = await GetFavoritesAsync();
                var favorite = favorites.FirstOrDefault(f => f.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

                if (favorite == null)
                    return false;

                favorite.Name = newName;
                await SaveFavoritesAsync(favorites);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task SaveFavoritesAsync(List<FavoriteFolder> favorites)
    {
        await Task.Run(() =>
        {
            try
            {
                var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_favoritesFilePath, json);
            }
            catch
            {
                // エラーは無視
            }
        });
    }
}

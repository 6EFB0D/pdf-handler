// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using PdfHandler.UI.Models;

namespace PdfHandler.UI.Services
{
    /// <summary>
    /// アップデート確認サービス
    /// </summary>
    public class UpdateChecker
    {
        private const string RELEASES_URL = "https://api.github.com/repos/6EFB0D/pdf-handler/releases/latest";
        private readonly HttpClient _httpClient;

        public UpdateChecker()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "PDFHandler");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// 更新を確認
        /// </summary>
        public async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                // GitHubから最新リリース情報を取得
                var response = await _httpClient.GetStringAsync(RELEASES_URL);
                var release = JsonSerializer.Deserialize<GitHubRelease>(response);

                if (release == null)
                {
                    return new UpdateInfo
                    {
                        HasError = true,
                        ErrorMessage = "リリース情報の解析に失敗しました。"
                    };
                }

                // バージョン比較
                var currentVersion = GetCurrentVersion();
                var latestVersion = ParseVersion(release.TagName);

                if (latestVersion == null)
                {
                    return new UpdateInfo
                    {
                        HasError = true,
                        ErrorMessage = "バージョン情報の解析に失敗しました。"
                    };
                }

                // 結果を返す
                return new UpdateInfo
                {
                    IsUpdateAvailable = latestVersion > currentVersion,
                    CurrentVersion = FormatVersion(currentVersion),
                    LatestVersion = FormatVersion(latestVersion),
                    ReleaseNotes = release.Body,
                    DownloadUrl = release.HtmlUrl,
                    ReleaseDate = release.PublishedAt,
                    AssetSize = release.Assets.Count > 0 ? release.Assets[0].Size : 0,
                    HasError = false
                };
            }
            catch (HttpRequestException ex)
            {
                return new UpdateInfo
                {
                    HasError = true,
                    ErrorMessage = "インターネット接続を確認してください。\n\n" +
                                   $"詳細: {ex.Message}"
                };
            }
            catch (TaskCanceledException)
            {
                return new UpdateInfo
                {
                    HasError = true,
                    ErrorMessage = "接続がタイムアウトしました。\n\n" +
                                   "ネットワーク環境を確認してください。"
                };
            }
            catch (Exception ex)
            {
                return new UpdateInfo
                {
                    HasError = true,
                    ErrorMessage = $"予期しないエラーが発生しました。\n\n詳細: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 現在のバージョンを取得
        /// </summary>
        private Version GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetName().Version ?? new Version(0, 0, 0);
        }

        /// <summary>
        /// タグ名からバージョンをパース
        /// </summary>
        private Version? ParseVersion(string tagName)
        {
            try
            {
                // "v4.0.0" → "4.0.0"
                var versionString = tagName.TrimStart('v', 'V');
                return Version.Parse(versionString);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// バージョンを表示用にフォーマット
        /// </summary>
        private string FormatVersion(Version version)
        {
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}

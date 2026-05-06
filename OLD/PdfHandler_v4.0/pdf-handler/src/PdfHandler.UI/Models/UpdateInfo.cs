// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace PdfHandler.UI.Models
{
    /// <summary>
    /// アップデート情報を保持するモデル
    /// </summary>
    public class UpdateInfo
    {
        /// <summary>
        /// 更新が利用可能かどうか
        /// </summary>
        public bool IsUpdateAvailable { get; set; }

        /// <summary>
        /// 現在のバージョン
        /// </summary>
        public string CurrentVersion { get; set; } = string.Empty;

        /// <summary>
        /// 最新バージョン
        /// </summary>
        public string LatestVersion { get; set; } = string.Empty;

        /// <summary>
        /// リリースノート（更新内容）
        /// </summary>
        public string ReleaseNotes { get; set; } = string.Empty;

        /// <summary>
        /// ダウンロードURL
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>
        /// リリース日
        /// </summary>
        public DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// ファイルサイズ（バイト）
        /// </summary>
        public long AssetSize { get; set; }

        /// <summary>
        /// エラーが発生したかどうか
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// エラーメッセージ
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// ファイルサイズを人間が読みやすい形式で取得
        /// </summary>
        public string FormattedSize
        {
            get
            {
                if (AssetSize == 0) return "不明";

                string[] sizes = { "B", "KB", "MB", "GB" };
                double size = AssetSize;
                int order = 0;

                while (size >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    size /= 1024;
                }

                return $"{size:0.#} {sizes[order]}";
            }
        }

        /// <summary>
        /// リリース日を日本語形式で取得
        /// </summary>
        public string FormattedReleaseDate
        {
            get
            {
                if (!ReleaseDate.HasValue) return "不明";
                return ReleaseDate.Value.ToString("yyyy年MM月dd日");
            }
        }
    }
}

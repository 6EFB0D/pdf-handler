// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using PdfHandler.Core.Interfaces;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Docnet.Core;
using Docnet.Core.Models;

namespace PdfHandler.Infrastructure.Services;

/// <summary>
/// PDF操作サービスの実装（Docnet.Core使用）
/// </summary>
public class PdfService : IPdfService
{
    private static readonly IDocLib _docLib = DocLib.Instance;

    /// <summary>
    /// PDFのページ数を取得
    /// </summary>
    public async Task<int> GetPageCountAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(filePath))
                    return 0;

                // ファイルをメモリに読み込み（ロック回避）
                byte[] fileBytes = File.ReadAllBytes(filePath);
                
                using var docReader = _docLib.GetDocReader(fileBytes, new PageDimensions(1080, 1920));
                return docReader.GetPageCount();
            }
            catch
            {
                return 0;
            }
        });
    }

    /// <summary>
    /// サムネイル画像を生成（第1ページ）
    /// </summary>
    public async Task<byte[]> GenerateThumbnailAsync(string filePath, int width = 150, int height = 200)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(filePath))
                    return CreatePlaceholderThumbnail(width, height);

                // ファイルをメモリに読み込み（ロック回避）
                byte[] fileBytes = File.ReadAllBytes(filePath);
                
                using var docReader = _docLib.GetDocReader(fileBytes, new PageDimensions(width, height));
                
                if (docReader.GetPageCount() == 0)
                    return CreatePlaceholderThumbnail(width, height);

                using var pageReader = docReader.GetPageReader(0);
                
                // ページをレンダリング（96 DPI）
                var rawBytes = pageReader.GetImage();
                var pageWidth = pageReader.GetPageWidth();
                var pageHeight = pageReader.GetPageHeight();

                // Bitmapに変換
                using var bitmap = new Bitmap(pageWidth, pageHeight, PixelFormat.Format32bppArgb);
                AddBytes(bitmap, rawBytes);

                // リサイズしてPNG形式で返す
                using var resized = ResizeImage(bitmap, width, height);
                using var ms = new MemoryStream();
                resized.Save(ms, ImageFormat.Png);
                
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                // デバッグ用：エラー内容をコンソールに出力
                System.Diagnostics.Debug.WriteLine($"サムネイル生成エラー [{Path.GetFileName(filePath)}]: {ex.Message}");
                return CreatePlaceholderThumbnail(width, height);
            }
        });
    }

    /// <summary>
    /// 指定ページをレンダリング
    /// </summary>
    public async Task<byte[]> RenderPageAsync(string filePath, int pageNumber, int dpi = 96)
    {
        // pageNumberは1ベース、内部では0ベースに変換
        int pageIndex = pageNumber - 1;
        
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(filePath))
                    return CreatePlaceholderPreview(800, 1000, pageNumber, filePath);

                // ファイルをメモリに読み込み（ロック回避）
                byte[] fileBytes = File.ReadAllBytes(filePath);
                
                // DPIに基づいてサイズを計算
                int renderWidth = (int)(8.27 * dpi);  // A4幅（インチ）× DPI
                int renderHeight = (int)(11.69 * dpi); // A4高さ（インチ）× DPI
                
                using var docReader = _docLib.GetDocReader(fileBytes, new PageDimensions(renderWidth, renderHeight));
                
                if (pageIndex < 0 || pageIndex >= docReader.GetPageCount())
                    return CreatePlaceholderPreview(800, 1000, pageNumber, filePath);

                using var pageReader = docReader.GetPageReader(pageIndex);
                
                // ページをレンダリング
                var rawBytes = pageReader.GetImage();
                var pageWidth = pageReader.GetPageWidth();
                var pageHeight = pageReader.GetPageHeight();

                // Bitmapに変換してPNG形式で返す
                using var bitmap = new Bitmap(pageWidth, pageHeight, PixelFormat.Format32bppArgb);
                AddBytes(bitmap, rawBytes);
                
                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                // デバッグ用：エラー内容をコンソールに出力
                System.Diagnostics.Debug.WriteLine($"ページレンダリングエラー [{Path.GetFileName(filePath)}] Page {pageNumber}: {ex.Message}");
                return CreatePlaceholderPreview(800, 1000, pageNumber, filePath);
            }
        });
    }

    /// <summary>
    /// PDFをメモリに読み込む（ファイルロック回避）
    /// </summary>
    public async Task<byte[]> LoadPdfToMemoryAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(filePath))
                    return Array.Empty<byte>();

                return File.ReadAllBytes(filePath);
            }
            catch
            {
                return Array.Empty<byte>();
            }
        });
    }

    /// <summary>
    /// バイト配列をBitmapに追加
    /// </summary>
    private static void AddBytes(Bitmap bmp, byte[] rawBytes)
    {
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
        var pNative = bmpData.Scan0;
        
        System.Runtime.InteropServices.Marshal.Copy(rawBytes, 0, pNative, rawBytes.Length);
        bmp.UnlockBits(bmpData);
    }

    /// <summary>
    /// 画像をリサイズ
    /// </summary>
    private static Bitmap ResizeImage(Image image, int width, int height)
    {
        // アスペクト比を維持
        double ratioX = (double)width / image.Width;
        double ratioY = (double)height / image.Height;
        double ratio = Math.Min(ratioX, ratioY);

        int newWidth = (int)(image.Width * ratio);
        int newHeight = (int)(image.Height * ratio);

        var destRect = new Rectangle(0, 0, newWidth, newHeight);
        var destImage = new Bitmap(newWidth, newHeight);

        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

        using (var graphics = Graphics.FromImage(destImage))
        {
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            using var wrapMode = new System.Drawing.Imaging.ImageAttributes();
            wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
            graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
        }

        return destImage;
    }

    /// <summary>
    /// プレースホルダーサムネイルを作成
    /// </summary>
    private byte[] CreatePlaceholderThumbnail(int width, int height)
    {
        try
        {
            using var bmp = new Bitmap(width, height);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.WhiteSmoke);
            
            // PDFアイコン風の描画
            var rect = new Rectangle(10, 10, width - 20, height - 40);
            g.FillRectangle(Brushes.White, rect);
            g.DrawRectangle(Pens.DarkGray, rect);
            
            // PDF テキスト
            using var font = new Font("Arial", 10, FontStyle.Bold);
            var text = "PDF";
            var textSize = g.MeasureString(text, font);
            g.DrawString(text, font, Brushes.DarkRed, 
                (width - textSize.Width) / 2, 
                (height - textSize.Height) / 2);

            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// プレースホルダープレビューを作成
    /// </summary>
    private byte[] CreatePlaceholderPreview(int width, int height, int pageNumber, string filePath)
    {
        try
        {
            using var bmp = new Bitmap(width, height);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            
            // ページ情報を描画
            using var font = new Font("Arial", 24);
            g.DrawString($"Page {pageNumber}", font, Brushes.Black, 20, 20);
            g.DrawString($"File: {Path.GetFileName(filePath)}", new Font("Arial", 12), Brushes.Gray, 20, 60);

            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }
}

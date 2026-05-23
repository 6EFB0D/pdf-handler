// PDFハンドラ (PDF Handler)
// Copyright (c) 2025-2026 Goplan. All rights reserved.

using System;
using System.IO;
using System.Text.Json;

namespace PdfHandler.UI.Services;

/// <summary>
/// 起動時更新通知の抑止状態（%AppData%\PDFHandler\update-notification.json）。
/// </summary>
public sealed class UpdateNotificationStore
{
    private readonly string _settingsPath;

    public UpdateNotificationStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "PDFHandler");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "update-notification.json");
    }

    public bool ShouldShowStartupNotification(string latestTagName)
    {
        if (string.IsNullOrWhiteSpace(latestTagName))
            return true;

        var state = Load();
        if (string.IsNullOrWhiteSpace(state.SuppressedThroughTag))
            return true;

        return ReleaseVersionHelper.IsNewerTag(latestTagName, state.SuppressedThroughTag);
    }

    public void SetSuppressedThroughTag(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return;

        var state = Load();
        state.SuppressedThroughTag = tagName.Trim();
        Save(state);
    }

    public void ClearSuppression()
    {
        Save(new UpdateNotificationState());
    }

    /// <summary>
    /// 「起動時の更新通知を再び表示」用。直後に同じ公開版の通知を出さないよう、
    /// 現在 GitHub 上の最新タグまでを済み扱いにする（それより新しい版が出たときだけ起動時通知）。
    /// </summary>
    public void ReEnableNotificationsThroughCurrentLatest(string? latestTagName)
    {
        if (string.IsNullOrWhiteSpace(latestTagName))
        {
            ClearSuppression();
            return;
        }

        SetSuppressedThroughTag(latestTagName.Trim());
    }

    public string? GetSuppressedThroughTag() => Load().SuppressedThroughTag;

    private UpdateNotificationState Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return new UpdateNotificationState();

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<UpdateNotificationState>(json)
                   ?? new UpdateNotificationState();
        }
        catch
        {
            return new UpdateNotificationState();
        }
    }

    private void Save(UpdateNotificationState state)
    {
        try
        {
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // 通知抑止の保存失敗は起動を妨げない
        }
    }

    private sealed class UpdateNotificationState
    {
        public string? SuppressedThroughTag { get; set; }
    }
}

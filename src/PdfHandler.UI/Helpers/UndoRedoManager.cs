// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace PdfHandler.UI.Helpers;

/// <summary>
/// PDFファイルの編集操作に対する元に戻す／やり直しを管理
/// </summary>
public class UndoRedoManager
{
    private const int MaxStackSize = 20;
    private readonly Stack<(string FilePath, string BackupPath)> _undoStack = new();
    private readonly Stack<(string FilePath, string BackupPath)> _redoStack = new();
    private readonly string _tempBaseDir;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public event Action? UndoRedoStateChanged;

    public UndoRedoManager()
    {
        _tempBaseDir = Path.Combine(Path.GetTempPath(), "PdfHandler", "Undo");
        if (!Directory.Exists(_tempBaseDir))
            Directory.CreateDirectory(_tempBaseDir);
    }

    /// <summary>
    /// 変更前に呼び出す。ファイルのバックアップを作成しUndoスタックに積む
    /// </summary>
    public bool PushUndo(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;
        try
        {
            var fileName = Path.GetFileName(filePath);
            var uniqueName = $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid():N}.pdf";
            var backupPath = Path.Combine(_tempBaseDir, uniqueName);
            File.Copy(filePath, backupPath, overwrite: true);
            _redoStack.Clear();
            _undoStack.Push((filePath, backupPath));
            while (_undoStack.Count > MaxStackSize)
            {
                var (_, oldBackup) = _undoStack.Pop();
                try { if (File.Exists(oldBackup)) File.Delete(oldBackup); } catch { /* ignore */ }
            }
            UndoRedoStateChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UndoRedoManager.PushUndo: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 直前のPushUndoを取り消す（操作失敗時に使用）
    /// </summary>
    public void CancelLastPush(string filePath)
    {
        if (_undoStack.Count == 0) return;
        var (path, backupPath) = _undoStack.Peek();
        if (!string.Equals(path, filePath, StringComparison.OrdinalIgnoreCase)) return;
        _undoStack.Pop();
        try { if (File.Exists(backupPath)) File.Delete(backupPath); } catch { /* ignore */ }
        UndoRedoStateChanged?.Invoke();
    }

    /// <summary>
    /// 元に戻す
    /// </summary>
    public bool Undo()
    {
        if (_undoStack.Count == 0) return false;
        try
        {
            var (filePath, undoBackup) = _undoStack.Pop();
            if (!File.Exists(undoBackup)) return false;

            var redoBackup = Path.Combine(_tempBaseDir, $"redo_{Guid.NewGuid():N}.pdf");
            File.Copy(filePath, redoBackup, overwrite: true);
            _redoStack.Push((filePath, redoBackup));

            File.Copy(undoBackup, filePath, overwrite: true);
            try { File.Delete(undoBackup); } catch { /* ignore */ }

            UndoRedoStateChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UndoRedoManager.Undo: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// やり直し
    /// </summary>
    public bool Redo()
    {
        if (_redoStack.Count == 0) return false;
        try
        {
            var (filePath, redoBackup) = _redoStack.Pop();
            if (!File.Exists(redoBackup)) return false;

            var undoBackup = Path.Combine(_tempBaseDir, $"undo_{Guid.NewGuid():N}.pdf");
            File.Copy(filePath, undoBackup, overwrite: true);
            _undoStack.Push((filePath, undoBackup));

            File.Copy(redoBackup, filePath, overwrite: true);
            try { File.Delete(redoBackup); } catch { /* ignore */ }

            UndoRedoStateChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UndoRedoManager.Redo: {ex.Message}");
            return false;
        }
    }
}

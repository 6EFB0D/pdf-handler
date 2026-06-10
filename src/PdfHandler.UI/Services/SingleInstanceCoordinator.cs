// PDFハンドラ (PDF Handler)
// Copyright (c) 2025-2026 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace PdfHandler.UI.Services;

/// <summary>
/// 単一インスタンス（Mutex）と既存ウィンドウの前面表示を扱う。
/// </summary>
internal static class SingleInstanceCoordinator
{
    public const string MutexName = "Goplan.PDFHandler.SingleInstance";
    private const int SwRestore = 9;

    internal enum StartupDecision
    {
        ContinueAsPrimary,
        ActivateExistingAndExit,
        ShowRecoveryMessageAndExit
    }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    /// <summary>
    /// 起動時の単一インスタンス判定。成功時は <paramref name="mutex"/> を保持し <paramref name="ownsMutex"/> が true。
    /// プロセス名での先行判定は行わない（終了直後のゾンビ PID で誤ブロックしないため）。
    /// </summary>
    internal static StartupDecision TryAcquirePrimaryInstance(out Mutex? mutex, out bool ownsMutex)
    {
        mutex = null;
        ownsMutex = false;

        try
        {
            var created = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
            if (createdNew)
            {
                mutex = created;
                ownsMutex = true;
                return StartupDecision.ContinueAsPrimary;
            }

            created.Dispose();
        }
        catch
        {
            // ignore — 下流で OpenExisting / 再作成を試す
        }

        if (TryTakeAbandonedMutex(out mutex))
        {
            ownsMutex = true;
            return StartupDecision.ContinueAsPrimary;
        }

        if (TryActivateExistingMainWindow())
            return StartupDecision.ActivateExistingAndExit;

        // Mutex は存在するが前面化できるウィンドウがない — 放棄 Mutex を再試行
        if (TryTakeAbandonedMutex(out mutex))
        {
            ownsMutex = true;
            return StartupDecision.ContinueAsPrimary;
        }

        return StartupDecision.ShowRecoveryMessageAndExit;
    }

    internal static void ReleaseMutex(ref Mutex? mutex)
    {
        if (mutex == null)
            return;

        try
        {
            if (mutex.SafeWaitHandle is { IsClosed: false })
                mutex.ReleaseMutex();
        }
        catch
        {
            // 既に解放済み
        }

        try
        {
            mutex.Dispose();
        }
        catch
        {
            // ignore
        }

        mutex = null;
    }

    private static bool TryTakeAbandonedMutex(out Mutex? mutex)
    {
        mutex = null;
        try
        {
            mutex = Mutex.OpenExisting(MutexName);
            if (mutex.WaitOne(0))
                return true;
        }
        catch (AbandonedMutexException)
        {
            // 前プロセス異常終了 — 所有権はこのスレッドに付与される
            return mutex != null;
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // Mutex が消えた直後 — 再作成
        }
        catch (UnauthorizedAccessException)
        {
            // ignore
        }

        mutex?.Dispose();
        mutex = null;

        try
        {
            mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
            if (createdNew)
                return true;
        }
        catch
        {
            // ignore
        }

        mutex?.Dispose();
        mutex = null;
        return false;
    }

    private static bool TryActivateExistingMainWindow()
    {
        IntPtr target = IntPtr.Zero;

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;

            var sb = new StringBuilder(512);
            _ = GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString();
            if (title.Contains("PDFハンドラ", StringComparison.Ordinal))
            {
                target = hWnd;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        if (target == IntPtr.Zero)
            return false;

        ShowWindow(target, SwRestore);
        return SetForegroundWindow(target);
    }
}

// PDFハンドラ (PDF Handler)
// Copyright (c) 2025-2026 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using PdfHandler.Core.Interfaces;
using PdfHandler.Infrastructure.Configuration;
using PdfHandler.Infrastructure.Services;
using PdfHandler.UI.ViewModels;
using PdfHandler.UI.Views;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PdfHandler.UI;

/// <summary>
/// App.xaml の相互作用ロジック
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private static Mutex? _singleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 単一インスタンス制限（二重起動・ロックの防止）
        const string mutexName = "Goplan.PDFHandler.SingleInstance";
        _singleInstanceMutex = new Mutex(true, mutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "PDFハンドラは既に起動しています。\n別のウィンドウで開いている場合は、そちらをご利用ください。",
                "二重起動",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // 未処理例外をキャッチ（アプリ終了の原因特定用）
        DispatcherUnhandledException += (s, args) =>
        {
            var msg = $"未処理の例外が発生しました:\n\n{args.Exception.GetType().Name}\n{args.Exception.Message}\n\n{args.Exception.StackTrace}";
            MessageBox.Show(msg, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            System.Diagnostics.Debug.WriteLine(msg);
            args.Handled = true; // アプリ終了を防ぐ
        };
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                var msg = $"[UnhandledException] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
                MessageBox.Show(msg, "致命的エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine(msg);
            }
        };
        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            var msg = $"[UnobservedTaskException] {args.Exception?.GetType().Name}: {args.Exception?.Message}";
            MessageBox.Show(msg, "タスク例外", MessageBoxButton.OK, MessageBoxImage.Warning);
            System.Diagnostics.Debug.WriteLine(msg);
            args.SetObserved();
        };

        try
        {
            // DIコンテナの設定
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();

            // メインウィンドウを表示
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"アプリケーションの起動に失敗しました。\n\nエラー: {ex.Message}\n\nスタックトレース:\n{ex.StackTrace}",
                "起動エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        var appSettings = new AppSettings();
        
        // Supabase設定（環境変数から読み込む、なければ開発用のデフォルト値を使用）
        appSettings.Supabase.AnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") 
            ?? "sb_publishable_ELiCbHZwAR-ekkwEvhzCcQ_mWWYB_-2"; // 開発用フォールバック値
        appSettings.Supabase.ServiceRoleKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY") 
            ?? ""; // Service Role Keyは機密情報のため、環境変数から読み込む必要がある
        
        // Premium版の公開設定（v1.0ではfalse）
        appSettings.EnablePremiumPlan = false;

        // お問い合わせ先URL（サポート・ボリュームライセンス共通。環境変数で上書き可能）
        // ※ リリース前: 下記のプレースホルダーを実際のURLに差し替えること。docs/RELEASE_CHECKLIST.md 参照
        appSettings.ContactUrl = Environment.GetEnvironmentVariable("CONTACT_URL")
            ?? "https://example.com/contact";
        
        services.AddSingleton(appSettings);

        // Services
        services.AddSingleton<IPdfService, PdfService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IPdfMergeService, PdfMergeService>();
        services.AddSingleton<IPdfSplitService, PdfSplitService>();
        services.AddSingleton<IFavoriteService, FavoriteService>();
        services.AddSingleton<ILicenseService>(sp => new LicenseService(sp.GetRequiredService<AppSettings>()));
        services.AddSingleton<IPdfRotateService, PdfRotateService>();
        services.AddSingleton<IPdfPageService, PdfPageService>();
        services.AddSingleton<IHeaderFooterService, HeaderFooterService>();
        services.AddSingleton<IWorkFolderService, WorkFolderService>();
        services.AddSingleton<IBinderService, BinderService>();
        services.AddSingleton<IPrintToPdfService, PrintToPdfService>();
        services.AddSingleton<IPaymentService>(sp => new PaymentService(sp.GetRequiredService<AppSettings>()));

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }

    /// <summary>
    /// サービスを取得（簡易DIヘルパー）
    /// </summary>
    public T GetService<T>() where T : class
    {
        return _serviceProvider?.GetRequiredService<T>() 
            ?? throw new InvalidOperationException("ServiceProvider is not initialized");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _singleInstanceMutex?.ReleaseMutex();
        }
        catch { /* 既に解放済みの可能性 */ }
        try
        {
            _singleInstanceMutex?.Dispose();
        }
        catch { }
        try
        {
            _serviceProvider?.Dispose();
        }
        catch { }
        base.OnExit(e);
    }
}

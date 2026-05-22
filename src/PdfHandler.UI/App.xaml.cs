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
using System.IO;
using System.Text.Json;
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

        // スプラッシュ画面を先に表示
        var splash = new Views.StartupSplashWindow();
        splash.Show();

        try
        {
            // DIコンテナの設定
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();

            // MainWindow（＝ViewModel）を構築（この時点で InitializeAsync が裏で走り始める）
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();

            // 初期化完了時にスプラッシュを閉じてメインウィンドウを表示
            viewModel.InitializationCompleted += (_, _) =>
            {
                mainWindow.Dispatcher.Invoke(() =>
                {
                    mainWindow.Show();
                    splash.Close();
                });
            };
        }
        catch (Exception ex)
        {
            splash.Close();
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

        ApplyRuntimeSettings(appSettings);
        
        // Supabase設定（環境変数または PdfHandler.runtime.json。秘密鍵はリポジトリに含めない）
        appSettings.Supabase.Url = Environment.GetEnvironmentVariable("SUPABASE_URL")
            ?? appSettings.Supabase.Url;
        appSettings.Supabase.AnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY")
            ?? appSettings.Supabase.AnonKey;
        appSettings.Supabase.ServiceRoleKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY") 
            ?? ""; // Service Role Keyは機密情報のため、環境変数から読み込む必要がある
        
        // お問い合わせ先URL（サポート・ボリュームライセンス共通。環境変数で上書き可能）
        // ※ リリースビルド: プレースホルダを実URLに（運用 CI / runtime.json）。
        appSettings.ContactUrl = Environment.GetEnvironmentVariable("CONTACT_URL")
            ?? appSettings.ContactUrl;

        // 商品紹介ページ・アンケートフォーム（環境変数で上書き可能）
        appSettings.ProductPageUrl = Environment.GetEnvironmentVariable("PRODUCT_PAGE_URL")
            ?? appSettings.ProductPageUrl;
        appSettings.SurveyFormUrl = Environment.GetEnvironmentVariable("SURVEY_FORM_URL")
            ?? appSettings.SurveyFormUrl;

        // HMACオフライン検証用の秘密鍵（LICENSE_SECRET_KEY が必須）
        appSettings.LicenseSecretKey = Environment.GetEnvironmentVariable("LICENSE_SECRET_KEY")
            ?? appSettings.LicenseSecretKey;

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

    private static void ApplyRuntimeSettings(AppSettings appSettings)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "PdfHandler.runtime.json");
        if (!File.Exists(configPath))
            return;

        try
        {
            var json = File.ReadAllText(configPath);
            var settings = JsonSerializer.Deserialize<RuntimeSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (settings == null)
                return;

            if (!string.IsNullOrWhiteSpace(settings.SupabaseUrl))
                appSettings.Supabase.Url = settings.SupabaseUrl.Trim();
            if (!string.IsNullOrWhiteSpace(settings.SupabaseAnonKey))
                appSettings.Supabase.AnonKey = settings.SupabaseAnonKey.Trim();
            if (!string.IsNullOrWhiteSpace(settings.ContactUrl))
                appSettings.ContactUrl = settings.ContactUrl.Trim();
            if (!string.IsNullOrWhiteSpace(settings.ProductPageUrl))
                appSettings.ProductPageUrl = settings.ProductPageUrl.Trim();
            if (!string.IsNullOrWhiteSpace(settings.SurveyFormUrl))
                appSettings.SurveyFormUrl = settings.SurveyFormUrl.Trim();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Runtime settings load failed: {ex.Message}");
        }
    }

    private sealed class RuntimeSettings
    {
        public string? SupabaseUrl { get; set; }
        public string? SupabaseAnonKey { get; set; }
        public string? ContactUrl { get; set; }
        public string? ProductPageUrl { get; set; }
        public string? SurveyFormUrl { get; set; }
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

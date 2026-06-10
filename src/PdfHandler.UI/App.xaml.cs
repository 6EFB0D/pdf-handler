// PDFハンドラ (PDF Handler)
// Copyright (c) 2025-2026 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using PdfHandler.Core.Interfaces;
using PdfHandler.Infrastructure.Configuration;
using PdfHandler.Infrastructure.Services;
using PdfHandler.UI.Services;
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
    private static bool _ownsSingleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        RegisterGlobalExceptionHandlers();

        var startupDecision = SingleInstanceCoordinator.TryAcquirePrimaryInstance(
            out _singleInstanceMutex,
            out _ownsSingleInstanceMutex);

        switch (startupDecision)
        {
            case SingleInstanceCoordinator.StartupDecision.ActivateExistingAndExit:
                Shutdown();
                return;

            case SingleInstanceCoordinator.StartupDecision.ShowRecoveryMessageAndExit:
                MessageBox.Show(
                    "PDFハンドラを起動できませんでした。\n\n" +
                    "前回の終了が不完全な可能性があります。\n" +
                    "• タスクマネージャーで「PdfHandler.UI」を終了してから再起動\n" +
                    "• それでも起動しない場合は PC を再起動\n\n" +
                    "改善しない場合はサポート（ヘルプ→お問い合わせ）までご連絡ください。",
                    "起動できません",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ReleaseSingleInstanceMutex();
                Shutdown();
                return;
        }

        Views.StartupSplashWindow? splash = null;
        try
        {
            splash = new Views.StartupSplashWindow();
            splash.Show();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();

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
            splash?.Close();
            MessageBox.Show(
                $"アプリケーションの起動に失敗しました。\n\nエラー: {ex.Message}\n\nスタックトレース:\n{ex.StackTrace}",
                "起動エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            ReleaseSingleInstanceMutex();
            Shutdown();
        }
    }

    private static void RegisterGlobalExceptionHandlers()
    {
        if (Current == null)
            return;

        Current.DispatcherUnhandledException += (_, args) =>
        {
            var msg = $"未処理の例外が発生しました:\n\n{args.Exception.GetType().Name}\n{args.Exception.Message}\n\n{args.Exception.StackTrace}";
            MessageBox.Show(msg, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            System.Diagnostics.Debug.WriteLine(msg);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                var msg = $"[UnhandledException] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
                MessageBox.Show(msg, "致命的エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine(msg);
            }

            ReleaseSingleInstanceMutex();
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            var msg = $"[UnobservedTaskException] {args.Exception?.GetType().Name}: {args.Exception?.Message}";
            MessageBox.Show(msg, "タスク例外", MessageBoxButton.OK, MessageBoxImage.Warning);
            System.Diagnostics.Debug.WriteLine(msg);
            args.SetObserved();
        };
    }

    private void ConfigureServices(IServiceCollection services)
    {
        var appSettings = new AppSettings();

        ApplyRuntimeSettings(appSettings);

        appSettings.Supabase.Url = Environment.GetEnvironmentVariable("SUPABASE_URL")
            ?? appSettings.Supabase.Url;
        appSettings.Supabase.AnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY")
            ?? appSettings.Supabase.AnonKey;
        appSettings.Supabase.ServiceRoleKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")
            ?? "";

        appSettings.ContactUrl = Environment.GetEnvironmentVariable("CONTACT_URL")
            ?? appSettings.ContactUrl;

        appSettings.ProductPageUrl = Environment.GetEnvironmentVariable("PRODUCT_PAGE_URL")
            ?? appSettings.ProductPageUrl;
        appSettings.HomePageUrl = Environment.GetEnvironmentVariable("HOME_PAGE_URL")
            ?? appSettings.HomePageUrl;
        appSettings.SurveyFormUrl = Environment.GetEnvironmentVariable("SURVEY_FORM_URL")
            ?? appSettings.SurveyFormUrl;

        appSettings.LicenseSecretKey = Environment.GetEnvironmentVariable("LICENSE_SECRET_KEY")
            ?? appSettings.LicenseSecretKey;

        AppEnvironmentResolver.FinalizeTargetEnvironment(appSettings);

        services.AddSingleton(appSettings);

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
        services.AddSingleton<ISupabaseConnectionTestService>(sp =>
            new SupabaseConnectionTestService(sp.GetRequiredService<AppSettings>()));

        services.AddSingleton<MainWindowViewModel>();
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
            if (!string.IsNullOrWhiteSpace(settings.HomePageUrl))
                appSettings.HomePageUrl = settings.HomePageUrl.Trim();
            if (!string.IsNullOrWhiteSpace(settings.SurveyFormUrl))
                appSettings.SurveyFormUrl = settings.SurveyFormUrl.Trim();
            if (!string.IsNullOrWhiteSpace(settings.TargetEnvironment))
                appSettings.TargetEnvironment = settings.TargetEnvironment.Trim();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Runtime settings load failed: {ex.Message}");
        }
    }

    private sealed class RuntimeSettings
    {
        public string? TargetEnvironment { get; set; }
        public string? SupabaseUrl { get; set; }
        public string? SupabaseAnonKey { get; set; }
        public string? ContactUrl { get; set; }
        public string? ProductPageUrl { get; set; }
        public string? HomePageUrl { get; set; }
        public string? SurveyFormUrl { get; set; }
    }

    public T GetService<T>() where T : class
    {
        return _serviceProvider?.GetRequiredService<T>()
            ?? throw new InvalidOperationException("ServiceProvider is not initialized");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ReleaseSingleInstanceMutex();

        try
        {
            _serviceProvider?.Dispose();
        }
        catch
        {
            // ignore
        }

        base.OnExit(e);
    }

    private static void ReleaseSingleInstanceMutex()
    {
        if (!_ownsSingleInstanceMutex || _singleInstanceMutex == null)
            return;

        SingleInstanceCoordinator.ReleaseMutex(ref _singleInstanceMutex);
        _ownsSingleInstanceMutex = false;
    }
}

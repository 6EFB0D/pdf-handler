// PDFハンドラ (PDF Handler)
// Copyright (c) 2025-2026 Goplan. All rights reserved.

using System.Reflection;
using System.Windows;

namespace PdfHandler.UI.Views
{
    public partial class StartupSplashWindow : Window
    {
        public StartupSplashWindow()
        {
            InitializeComponent();

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            VersionText.Text = version != null
                ? $"v{version.Major}.{version.Minor}.{version.Build}"
                : string.Empty;
        }

        public void SetStatus(string message)
        {
            Dispatcher.Invoke(() => StatusText.Text = message);
        }
    }
}

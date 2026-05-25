using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using POS.Desktop.Shell;

namespace POS.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly WebViewHost _webViewHost;

        public MainWindow(IConfiguration configuration, ILogger<WebViewHost> logger)
        {
            InitializeComponent();
            ApplyWindowIcon();
            _webViewHost = new WebViewHost(MainWebView, configuration, logger);
            SourceInitialized += MainWindow_SourceInitialized;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            FitToDesktopWorkArea();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _webViewHost.InitializeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 initialization failed: {ex.Message}\n\nThe application will now shut down.", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void FitToDesktopWorkArea()
        {
            Rect workArea = SystemParameters.WorkArea;

            WindowState = WindowState.Normal;
            Left = workArea.Left;
            Top = workArea.Top;
            Width = workArea.Width;
            Height = workArea.Height;
        }

        private void ApplyWindowIcon()
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "ui", "logo.png");

            if (!File.Exists(iconPath))
            {
                return;
            }

            Icon = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
        }
    }
}

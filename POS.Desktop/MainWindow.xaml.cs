using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
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

        public MainWindow(IConfiguration configuration, ILogger<WebViewHost> logger, PosWebMessageRouter router)
        {
            InitializeComponent();
            ApplyWindowIcon();
            _webViewHost = new WebViewHost(MainWebView, configuration, logger, router);
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

            BitmapImage source = new();
            source.BeginInit();
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.UriSource = new Uri(iconPath, UriKind.Absolute);
            source.EndInit();
            source.Freeze();

            Icon = CreateTaskbarIcon(source, 64);
        }

        private static ImageSource CreateTaskbarIcon(BitmapSource source, int iconSize)
        {
            DrawingVisual visual = new();

            using (DrawingContext context = visual.RenderOpen())
            {
                context.DrawRectangle(Brushes.White, null, new Rect(0, 0, iconSize, iconSize));

                Rect logoBounds = GetContainBounds(source.PixelWidth, source.PixelHeight, iconSize);
                context.DrawImage(source, logoBounds);
            }

            RenderTargetBitmap bitmap = new(iconSize, iconSize, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();

            return bitmap;
        }

        private static Rect GetContainBounds(double sourceWidth, double sourceHeight, double iconSize)
        {
            const double paddingRatio = 0.14;
            double maxSize = iconSize * (1 - (paddingRatio * 2));
            double scale = Math.Min(maxSize / sourceWidth, maxSize / sourceHeight);
            double width = sourceWidth * scale;
            double height = sourceHeight * scale;
            double left = (iconSize - width) / 2;
            double top = (iconSize - height) / 2;

            return new Rect(left, top, width, height);
        }
    }
}

using System.Windows;
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
            _webViewHost = new WebViewHost(MainWebView, configuration, logger);
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _webViewHost.InitializeAsync();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"WebView2 initialization failed: {ex.Message}\n\nThe application will now shut down.", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }
    }
}
using System.Windows;
using Microsoft.Extensions.Configuration;
using POS.Desktop.Shell;

namespace POS.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly WebViewHost _webViewHost;

        public MainWindow(IConfiguration configuration)
        {
            InitializeComponent();
            _webViewHost = new WebViewHost(MainWebView, configuration);
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
                MessageBox.Show($"WebView2 initialization failed: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
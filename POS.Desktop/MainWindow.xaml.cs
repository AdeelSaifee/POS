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
        }
    }
}
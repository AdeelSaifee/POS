using System.Windows;
using POS.Desktop.Shell;

namespace POS.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly WebViewHost _webViewHost;

        public MainWindow()
        {
            InitializeComponent();
            _webViewHost = new WebViewHost(MainWebView);
        }
    }
}
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace POS.Desktop.Shell;

/// <summary>
/// Responsible for orchestrating the WebView2 lifecycle, configuration, and JS-to-C# bridge.
/// </summary>
public sealed class WebViewHost
{
    private readonly WebView2 _webView;
    private readonly IConfiguration _configuration;
    private bool _isInitialized;

    public WebViewHost(WebView2 webView, IConfiguration configuration)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Task 1.3.5: Asynchronously initializes the CoreWebView2 environment.
    /// </summary>
    public async Task InitializeAsync()
    {
        var userDataFolder = ConfigureUserDataFolder();

        if (!Directory.Exists(userDataFolder))
        {
            Directory.CreateDirectory(userDataFolder);
        }

        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);

        await _webView.EnsureCoreWebView2Async(environment);

        _isInitialized = true;

        // TODO: ConfigureVirtualHostMapping (Phase 2)
        // TODO: RegisterMessageBridge (Phase 3)
        // TODO: NavigateToInitialScreen (Phase 2)
    }

    /// <summary>
    /// Guard to ensure WebView2 is initialized before dependent operations.
    /// </summary>
    private void EnsureInitialized()
    {
        if (!_isInitialized || _webView.CoreWebView2 is null)
        {
            throw new InvalidOperationException("WebView2 must be initialized before navigation or bridge operations.");
        }
    }

    /// <summary>
    /// Task 1.3.4: Defines the location for the WebView2 user data folder.
    /// Resolves to %LocalAppData% + WebView2:UserDataFolder configuration.
    /// </summary>
    private string ConfigureUserDataFolder()
    {
        var subFolder = _configuration["WebView2:UserDataFolder"] ?? "IMAGYN/POS/Desktop/WebView2";
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, subFolder);
    }

    /// <summary>
    /// TODO Phase 2: Sets up virtual host mapping to serve local assets.
    /// </summary>
    private void ConfigureVirtualHostMapping()
    {
        EnsureInitialized();
    }

    /// <summary>
    /// TODO Phase 3: Registers the JS-to-C# message bridge.
    /// </summary>
    private void RegisterMessageBridge()
    {
        EnsureInitialized();
    }

    /// <summary>
    /// TODO Phase 2: Navigates to the initial application screen.
    /// </summary>
    private void NavigateToInitialScreen()
    {
        EnsureInitialized();
    }
}

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace POS.Desktop.Shell;

/// <summary>
/// Responsible for orchestrating the WebView2 lifecycle, configuration, and JS-to-C# bridge.
/// </summary>
public sealed class WebViewHost
{
    private const string AppHost = "pos.app";
    private const string AppOrigin = $"https://{AppHost}/";

    private readonly WebView2 _webView;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebViewHost> _logger;
    private bool _isInitialized;
    private bool _isWebMessageHandlerRegistered;

    public WebViewHost(WebView2 webView, IConfiguration configuration, ILogger<WebViewHost> logger)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Asynchronously initializes the CoreWebView2 environment.
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing WebView2 environment...");

        try
        {
            var userDataFolder = ConfigureUserDataFolder();
            _logger.LogDebug("WebView2 user data folder: {UserDataFolder}", userDataFolder);

            if (!Directory.Exists(userDataFolder))
            {
                _logger.LogInformation("Creating missing WebView2 user data folder...");
                Directory.CreateDirectory(userDataFolder);
            }

            _logger.LogInformation("Creating CoreWebView2 environment...");
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);

            _logger.LogInformation("Ensuring CoreWebView2 control is ready...");
            await _webView.EnsureCoreWebView2Async(environment);

            _isInitialized = true;
            _logger.LogInformation("WebView2 shell initialized successfully.");

            ConfigureVirtualHostMapping();

            RegisterWebMessageHandler();

            NavigateToInitialScreen();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to initialize WebView2 shell component.");
            throw;
        }
    }

    /// <summary>
    /// Resolves the absolute runtime path to the UI assets folder.
    /// Uses AppContext.BaseDirectory to ensure the path is relative to the application output directory.
    /// </summary>
    private string GetAssetsUiPath()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "ui");

        if (!Directory.Exists(path))
        {
            var error = $"UI assets folder missing at: {path}. Ensure project Content items are correctly configured.";
            _logger.LogError(error);
            throw new DirectoryNotFoundException(error);
        }

        return path;
    }

    /// <summary>
    /// Renders a minimal placeholder page to verify WebView2 rendering capabilities.
    /// </summary>
    private void RenderPlaceholderPage()
    {
        EnsureInitialized();

        _logger.LogInformation("Rendering shell placeholder page...");

        const string html = @"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body { 
                        background-color: #202020; 
                        color: #A8E63D; 
                        font-family: sans-serif; 
                        display: flex; 
                        flex-direction: column; 
                        justify-content: center; 
                        align-items: center; 
                        height: 100vh; 
                        margin: 0; 
                    }
                    h1 { font-size: 3rem; margin-bottom: 1rem; }
                    p { font-size: 1.5rem; opacity: 0.8; }
                </style>
            </head>
            <body>
                <h1>IMAGYN POS Desktop Shell</h1>
                <p>WebView2 initialized successfully.</p>
            </body>
            </html>";

        _webView.CoreWebView2.NavigateToString(html);
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
    /// Defines the location for the WebView2 user data folder.
    /// Resolves to %LocalAppData% + WebView2:UserDataFolder configuration.
    /// </summary>
    private string ConfigureUserDataFolder()
    {
        var subFolder = _configuration["WebView2:UserDataFolder"] ?? "IMAGYN/POS/Desktop/WebView2";
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, subFolder);
    }

    /// <summary>
    /// Sets up virtual host mapping to serve local UI assets through a stable origin.
    /// Maps 'https://pos.app/' to the local 'Assets/ui/' folder.
    /// </summary>
    private void ConfigureVirtualHostMapping()
    {
        EnsureInitialized();

        var assetsPath = GetAssetsUiPath();

        _logger.LogInformation("Configuring virtual host mapping for {HostName} to {AssetsPath}", AppHost, assetsPath);

        _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            AppHost,
            assetsPath,
            CoreWebView2HostResourceAccessKind.Allow);

        _logger.LogInformation("Virtual host mapping active for {AppOrigin}", AppOrigin);
    }

    /// <summary>
    /// Registers the JS-to-C# message bridge transport hook.
    /// </summary>
    private void RegisterWebMessageHandler()
    {
        EnsureInitialized();

        if (_isWebMessageHandlerRegistered) return;

        _logger.LogInformation("Registering WebView2 WebMessageReceived handler.");
        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        _isWebMessageHandlerRegistered = true;
    }

    /// <summary>
    /// Handles inbound messages from the WebView2 content.
    /// </summary>
    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // Minimal transport-level logging to verify reachability.
        // Full payload logging is avoided to prevent accidental PII/credential exposure.
        _logger.LogInformation("Received WebView2 message from {Source}.", e.Source);

        try
        {
            // TODO Phase 3.3: Dispatch to PosWebMessageRouter
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in WebView2 message handler.");
        }
    }

    /// <summary>
    /// Navigates to the initial application screen.
    /// Uses the standardized AppOrigin and virtual host mapping.
    /// </summary>
    private void NavigateToInitialScreen()
    {
        EnsureInitialized();

        var initialUrl = $"{AppOrigin}terminal_login.html";
        _logger.LogInformation("Navigating to initial screen: {InitialUrl}", initialUrl);

        _webView.CoreWebView2.Navigate(initialUrl);
    }
}

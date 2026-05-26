using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using POS.Desktop.Bridge;

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
    private bool _isPosHostObjectRegistered;

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

            RegisterPosHostObject();

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
    /// Registers the COM-visible host object for the WebView2 JS-to-C# bridge.
    /// </summary>
    private void RegisterPosHostObject()
    {
        EnsureInitialized();

        if (_isPosHostObjectRegistered) return;

        _logger.LogInformation("Registering 'pos' host object for JS bridge.");
        _webView.CoreWebView2.AddHostObjectToScript("pos", new PosHostApi());
        _isPosHostObjectRegistered = true;
    }

    /// <summary>
    /// Handles inbound messages from the WebView2 content.
    /// </summary>
    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // Extract required data while still on the UI thread and before any async suspension.
        // This ensures the event args remain valid for the extraction.
        var rawJson = e.WebMessageAsJson;
        var source = e.Source;

        _logger.LogInformation("Received WebView2 message from {Source}.", source);

        try
        {
            // Delegate to an async handler to allow future non-blocking service dispatch.
            await HandleWebMessageAsync(rawJson, source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in WebView2 message handler for source {Source}.", source);
        }
    }

    /// <summary>
    /// Asynchronously processes the web message and dispatches to the appropriate handler.
    /// </summary>
    private async Task HandleWebMessageAsync(string rawJson, string source)
    {
        // Task 3.1.6: Marshal handlers correctly.
        // Task 3.1.8: Basic message logging.
        // Task 3.2.7: Handle malformed messages.
        // Task 3.2.8: Handle unknown message types.

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            // 1. Detect Message Type and identify if it is a Legacy Probe or v1 Envelope.
            string? messageType = null;
            if (root.TryGetProperty("type", out var typeElement))
            {
                messageType = typeElement.GetString();
            }

            // 2. Handle Legacy Milestone 3.1 Transport Probe (transport.ping).
            // This does not require version or requestId.
            if (messageType == "transport.ping")
            {
                await HandleLegacyPingAsync(source);
                return;
            }

            // 3. Validate v1 Envelope Shape.
            if (!TryValidateV1Envelope(root, out var requestId, out var versionError))
            {
                _logger.LogWarning("Inbound malformed bridge request from {Source}. Reason: {Reason}", source, versionError);

                // Fallback to "unknown" type if extraction failed.
                var safeType = messageType ?? "unknown";
                var safeId = requestId ?? "unrecognized";

                await SendBridgeErrorAsync(safeType, safeId, "MALFORMED_REQUEST", "The message envelope was invalid.", source);
                return;
            }

            _logger.LogDebug("Inbound bridge message [Type: {Type}] from {Source}", messageType, source);

            // 4. Dispatch to Handlers (Task 3.2.6 Echo & Task 3.2.8 Unknown Types).
            switch (messageType)
            {
                case "transport.echo":
                    await HandleTransportEchoAsync(root, requestId!, source);
                    break;

                default:
                    _logger.LogWarning("Unsupported bridge message type '{Type}' from {Source}", messageType, source);
                    await SendBridgeErrorAsync(messageType!, requestId!, "UNSUPPORTED_TYPE", "The requested action is not implemented.", source, new { type = messageType });
                    break;
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse raw JSON bridge message from {Source}.", source);

            // Task 3.2.7: Malformed JSON recovery.
            await SendBridgeErrorAsync("unknown", "unrecognized", "MALFORMED_REQUEST", "The message envelope was invalid.", source);
        }
    }

    /// <summary>
    /// Validates the basic requirements of a v1 bridge envelope.
    /// </summary>
    private bool TryValidateV1Envelope(System.Text.Json.JsonElement root, out string? requestId, out string? error)
    {
        requestId = null;
        error = null;

        if (root.TryGetProperty("requestId", out var idElement))
        {
            requestId = idElement.GetString();
        }

        if (!root.TryGetProperty("version", out var verElement) || verElement.GetString() != BridgeEnvelopeVersion.V1)
        {
            error = "Missing or unsupported version.";
            return false;
        }

        if (!root.TryGetProperty("type", out var typeElement) || string.IsNullOrWhiteSpace(typeElement.GetString()))
        {
            error = "Missing or empty message type.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(requestId))
        {
            error = "Missing or empty requestId.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Handles the legacy transport.ping probe from Milestone 3.1.
    /// </summary>
    private async Task HandleLegacyPingAsync(string source)
    {
        var pong = new
        {
            type = "transport.pong",
            source = "desktop-shell",
            receivedType = "transport.ping",
            timestamp = DateTime.UtcNow.ToString("O")
        };

        var responseJson = System.Text.Json.JsonSerializer.Serialize(pong);

        await _webView.Dispatcher.InvokeAsync(() =>
        {
            _webView.CoreWebView2.PostWebMessageAsJson(responseJson);
        });

        _logger.LogDebug("Outbound bridge legacy pong to {Source}", source);
    }

    /// <summary>
    /// Handles the v1 transport.echo verification request.
    /// </summary>
    private async Task HandleTransportEchoAsync(System.Text.Json.JsonElement root, string requestId, string source)
    {
        var response = BridgeResponseEnvelope.Success(
            type: "transport.echo",
            requestId: requestId,
            payload: new { message = "echo", receivedType = "transport.echo" }
        );

        await SendBridgeResponseAsync(response, source);
    }

    /// <summary>
    /// Marshals and sends a structured v1 bridge response to the UI.
    /// </summary>
    private async Task SendBridgeResponseAsync(BridgeResponseEnvelope response, string source)
    {
        var responseJson = System.Text.Json.JsonSerializer.Serialize(response, BridgeJsonSerializerOptions.Default);

        await _webView.Dispatcher.InvokeAsync(() =>
        {
            _webView.CoreWebView2.PostWebMessageAsJson(responseJson);
        });

        if (response.Ok)
        {
            _logger.LogDebug("Outbound bridge response [Type: {Type}, RequestId: {RequestId}] to {Source}", response.Type, response.RequestId, source);
        }
        else
        {
            _logger.LogWarning("Outbound bridge error [Type: {Type}, RequestId: {RequestId}, Code: {Code}] to {Source}", response.Type, response.RequestId, response.Error?.Code, source);
        }
    }

    /// <summary>
    /// Creates and sends a v1 bridge error response.
    /// </summary>
    private async Task SendBridgeErrorAsync(string type, string requestId, string code, string message, string source, object? details = null)
    {
        var response = BridgeResponseEnvelope.Failure(type, requestId, code, message, details);
        await SendBridgeResponseAsync(response, source);
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

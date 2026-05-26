using System.Runtime.InteropServices;

namespace POS.Desktop.Shell;

/// <summary>
/// The COM-visible host object skeleton for the WebView2 JS-to-C# bridge.
/// This class will be exposed to JavaScript as 'window.chrome.webview.hostObjects.pos'.
/// 
/// Note: This object is intentionally NOT registered in WebView2 until Task 3.1.3.
/// It currently contains no business logic.
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.AutoDual)]
public sealed class PosHostApi
{
    /// <summary>
    /// A minimal skeleton status method for bridge verification.
    /// </summary>
    /// <returns>A status string indicating the host object is ready.</returns>
    public string GetBridgeStatus()
    {
        return "PosHostApi ready";
    }
}

using System;
using Microsoft.Web.WebView2.Core;

namespace POS.Desktop.Shell;

/// <summary>
/// Provides a minimal mechanism to detect the presence of the WebView2 runtime.
/// </summary>
public static class WebView2RuntimeGuard
{
    public const string FallbackTitle = "POS Startup Requirement";

    public const string FallbackMessage = 
        "IMAGYN POS cannot start because the Microsoft Edge WebView2 Runtime is not installed or is not available on this device.\n\n" +
        "Please install or repair the Microsoft Edge WebView2 Evergreen Runtime, then restart the POS application.\n\n" +
        "If this problem continues, contact technical support.";

    /// <summary>
    /// Represents the result of a WebView2 runtime presence check.
    /// </summary>
    public record RuntimeStatus(bool IsAvailable, string? Version = null, string? ErrorMessage = null);

    /// <summary>
    /// Detects the WebView2 runtime availability using CoreWebView2Environment.GetAvailableBrowserVersionString.
    /// </summary>
    /// <returns>A RuntimeStatus object indicating availability and version/error details.</returns>
    public static RuntimeStatus GetRuntimeStatus()
    {
        try
        {
            // Task 1.5.1: Use GetAvailableBrowserVersionString to detect the runtime.
            string version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            return new RuntimeStatus(true, Version: version);
        }
        catch (Exception ex)
        {
            // Convert any exception (e.g., WebView2RuntimeNotFoundException) into a safe result.
            return new RuntimeStatus(false, ErrorMessage: ex.Message);
        }
    }
}

using System;
using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace POS.Desktop.Tests.Shell;

/// <summary>
/// Static file-content checks for payment_screen.html (Task 5.4.9).
/// These tests catch regressions that compile cleanly but break runtime behavior.
/// </summary>
public class PaymentScreenStaticTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();
    private static readonly string AssetPath = Path.Combine(SolutionRoot, "POS.Desktop", "Assets", "ui", "payment_screen.html");
    private static readonly string PrototypePath = Path.Combine(SolutionRoot, "docs", "ui-prototype", "screens", "payment_screen.html");

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "POS.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Could not locate solution root from test base directory.");
    }

    private static string AssetHtml => File.ReadAllText(AssetPath);

    // ── Forbidden patterns ────────────────────────────────────────────────────

    [Fact]
    public void PaymentScreen_DoesNotRead_SessionStorageCart()
    {
        Assert.DoesNotContain("sessionStorage.getItem('pos_cart')", AssetHtml);
        Assert.DoesNotContain("sessionStorage.getItem(\"pos_cart\")", AssetHtml);
    }

    [Fact]
    public void PaymentScreen_DoesNotContain_SimulateCardPay()
    {
        Assert.DoesNotContain("simulateCardPay", AssetHtml);
    }

    [Fact]
    public void PaymentScreen_DoesNotContain_SimulateWalletPay()
    {
        Assert.DoesNotContain("simulateWalletPay", AssetHtml);
    }

    [Fact]
    public void PaymentScreen_DoesNotContain_FakeApprovalTimeout()
    {
        // The banned pattern is a setTimeout that resolves a fake card/wallet approval
        // (1800ms or 2200ms delay from the original simulation).
        // Legitimate timeouts (toast dismiss, shift redirect) remain allowed.
        Assert.DoesNotContain(", 1800)", AssetHtml);
        Assert.DoesNotContain(", 2200)", AssetHtml);
    }

    [Fact]
    public void PaymentScreen_DoesNotWrite_SessionStorageCart()
    {
        Assert.DoesNotContain("sessionStorage.setItem('pos_cart'", AssetHtml);
        Assert.DoesNotContain("sessionStorage.setItem(\"pos_cart\"", AssetHtml);
    }

    // ── Required bridge calls ─────────────────────────────────────────────────

    [Fact]
    public void PaymentScreen_CallsBridge_OrderGetCart()
    {
        Assert.Contains("'order.getCart'", AssetHtml);
    }

    [Fact]
    public void PaymentScreen_CallsBridge_PaymentGetTenderMethods()
    {
        Assert.Contains("'payment.getTenderMethods'", AssetHtml);
    }

    [Fact]
    public void PaymentScreen_CallsBridge_PaymentComplete()
    {
        Assert.Contains("'payment.complete'", AssetHtml);
    }

    // ── Stub functions present ────────────────────────────────────────────────

    [Fact]
    public void PaymentScreen_Defines_ApproveCardStub()
    {
        Assert.Contains("approveCardStub", AssetHtml);
    }

    [Fact]
    public void PaymentScreen_Defines_ApproveWalletStub()
    {
        Assert.Contains("approveWalletStub", AssetHtml);
    }

    // ── Robust tender resolution helpers present ──────────────────────────────

    [Fact]
    public void PaymentScreen_Defines_GetCashTenderMethod()
    {
        Assert.Contains("getCashTenderMethod", AssetHtml);
    }

    [Fact]
    public void PaymentScreen_Defines_GetCardTenderMethod()
    {
        Assert.Contains("getCardTenderMethod", AssetHtml);
    }

    [Fact]
    public void PaymentScreen_Defines_GetWalletTenderMethod()
    {
        Assert.Contains("getWalletTenderMethod", AssetHtml);
    }

    // ── Stable external reference helpers present ─────────────────────────────

    [Fact]
    public void PaymentScreen_Defines_GetOrCreateCardRef()
    {
        Assert.Contains("getOrCreateCardRef", AssetHtml);
    }

    [Fact]
    public void PaymentScreen_Defines_GetOrCreateWalletRef()
    {
        Assert.Contains("getOrCreateWalletRef", AssetHtml);
    }

    [Fact]
    public void PaymentScreen_CardRef_UsesExpectedPrefix()
    {
        Assert.Contains("TXN-CARD-", AssetHtml);
    }

    [Fact]
    public void PaymentScreen_WalletRef_UsesExpectedPrefix()
    {
        Assert.Contains("TXN-WALLET-", AssetHtml);
    }

    // ── Wallet fallback logic present ─────────────────────────────────────────

    [Fact]
    public void PaymentScreen_WalletFallback_ReturnsCardTenderMethod()
    {
        // getWalletTenderMethod must fall back to getCardTenderMethod
        Assert.Contains("getWalletTenderMethod", AssetHtml);
        // The function body must reference getCardTenderMethod as the fallback
        var idx = AssetHtml.IndexOf("function getWalletTenderMethod", StringComparison.Ordinal);
        Assert.True(idx >= 0, "getWalletTenderMethod function not found.");
        var fnBody = AssetHtml.Substring(idx, 200);
        Assert.Contains("getCardTenderMethod", fnBody);
    }

    // ── Idempotency reset breadth ─────────────────────────────────────────────

    [Fact]
    public void PaymentScreen_NumPress_ResetsIdempotencyOnEveryKey()
    {
        // resetIdempotencyKey() must appear inside numPress body (not just on 'clear')
        var idx = AssetHtml.IndexOf("function numPress", StringComparison.Ordinal);
        Assert.True(idx >= 0, "numPress function not found.");
        var fnBody = AssetHtml.Substring(idx, 350);
        // Must not be gated only on 'clear' — must appear after the if/else block
        Assert.Contains("resetIdempotencyKey", fnBody);
        // The call must NOT be inside the 'clear' branch string
        Assert.DoesNotContain("'clear') { rawInput = ''; resetIdempotencyKey", fnBody);
    }

    [Fact]
    public void PaymentScreen_CalcSplit_ResetsIdempotency()
    {
        var idx = AssetHtml.IndexOf("function calcSplit", StringComparison.Ordinal);
        Assert.True(idx >= 0, "calcSplit function not found.");
        var fnBody = AssetHtml.Substring(idx, 200);
        Assert.Contains("resetIdempotencyKey", fnBody);
    }

    [Fact]
    public void PaymentScreen_WalletPhone_HasOninputReset()
    {
        Assert.Contains("id=\"wallet-phone\"", AssetHtml);
        var idx = AssetHtml.IndexOf("id=\"wallet-phone\"", StringComparison.Ordinal);
        // Within the input element (~300 chars) the oninput reset must be present
        var element = AssetHtml.Substring(idx, 300);
        Assert.Contains("resetIdempotencyKey", element);
    }

    // ── Asset / prototype parity ──────────────────────────────────────────────

    [Fact]
    public void PaymentScreen_AssetAndPrototype_AreIdentical()
    {
        var assetBytes = File.ReadAllBytes(AssetPath);
        var protoBytes = File.ReadAllBytes(PrototypePath);

        using var sha = SHA256.Create();
        var assetHash = Convert.ToHexString(sha.ComputeHash(assetBytes));

        using var sha2 = SHA256.Create();
        var protoHash = Convert.ToHexString(sha2.ComputeHash(protoBytes));

        Assert.Equal(assetHash, protoHash);
    }
}

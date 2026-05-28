using System;
using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace POS.Desktop.Tests.Shell;

/// <summary>
/// Static file-content checks for cash_control.html (Task 5.5.8).
/// Asserts sync parity, exclusions of sessionStorage, and bridge endpoint registrations.
/// </summary>
public class CashControlScreenStaticTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();
    private static readonly string AssetPath = Path.Combine(SolutionRoot, "POS.Desktop", "Assets", "ui", "cash_control.html");
    private static readonly string PrototypePath = Path.Combine(SolutionRoot, "docs", "ui-prototype", "screens", "cash_control.html");

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "POS.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Could not locate solution root from test base directory.");
    }

    private static string AssetHtml => File.ReadAllText(AssetPath);

    // ── Asset / prototype parity ──────────────────────────────────────────────

    [Fact]
    public void CashControlScreen_AssetAndPrototype_AreIdentical()
    {
        var assetBytes = File.ReadAllBytes(AssetPath);
        var protoBytes = File.ReadAllBytes(PrototypePath);

        using var sha = SHA256.Create();
        var assetHash = Convert.ToHexString(sha.ComputeHash(assetBytes));

        using var sha2 = SHA256.Create();
        var protoHash = Convert.ToHexString(sha2.ComputeHash(protoBytes));

        Assert.Equal(assetHash, protoHash);
    }

    // ── Forbidden patterns ────────────────────────────────────────────────────

    [Fact]
    public void CashControlScreen_DoesNotReadOrWrite_SessionStorage_SafeDrops()
    {
        Assert.DoesNotContain("sessionStorage.getItem('pos_safe_drops')", AssetHtml);
        Assert.DoesNotContain("sessionStorage.getItem(\"pos_safe_drops\")", AssetHtml);
        Assert.DoesNotContain("sessionStorage.setItem('pos_safe_drops'", AssetHtml);
        Assert.DoesNotContain("sessionStorage.setItem(\"pos_safe_drops\"", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_DoesNotUse_SessionStorage_ShiftFloat_AsSourceOfTruth()
    {
        // Must not use sessionStorage.getItem('pos_shift_float') for active calculations
        Assert.DoesNotContain("sessionStorage.getItem('pos_shift_float')", AssetHtml);
        Assert.DoesNotContain("sessionStorage.getItem(\"pos_shift_float\")", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_DoesNotUse_SessionStorage_CompletedTransactions()
    {
        Assert.DoesNotContain("sessionStorage.getItem('pos_completed_transactions')", AssetHtml);
        Assert.DoesNotContain("sessionStorage.getItem(\"pos_completed_transactions\")", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_ManagerPin_IsNotPersisted()
    {
        // Verify managerPin is not written to localStorage or sessionStorage
        Assert.DoesNotContain("sessionStorage.setItem('managerPin'", AssetHtml);
        Assert.DoesNotContain("sessionStorage.setItem(\"managerPin\"", AssetHtml);
        Assert.DoesNotContain("localStorage.setItem('managerPin'", AssetHtml);
        Assert.DoesNotContain("localStorage.setItem(\"managerPin\"", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_DoesNotContain_HardcodedPinCheck()
    {
        // Original file had a check like "pin !== '1234'"
        Assert.DoesNotContain("!== '1234'", AssetHtml);
        Assert.DoesNotContain("!== \"1234\"", AssetHtml);
        Assert.DoesNotContain("=== '1234'", AssetHtml);
        Assert.DoesNotContain("=== \"1234\"", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_DoesNotContain_FakeRefIdGenerator()
    {
        // Reference generators like DRP- or INJ- with random numbers are removed
        Assert.DoesNotContain("'DRP-'", AssetHtml);
        Assert.DoesNotContain("\"DRP-\"", AssetHtml);
        Assert.DoesNotContain("'INJ-'", AssetHtml);
        Assert.DoesNotContain("\"INJ-\"", AssetHtml);
    }

    // ── Required bridge calls ─────────────────────────────────────────────────

    [Fact]
    public void CashControlScreen_CallsBridge_GetSummary()
    {
        Assert.Contains("cash.getSummary", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_CallsBridge_GetLedger()
    {
        Assert.Contains("cash.getLedger", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_CallsBridge_GetReasonCodes()
    {
        Assert.Contains("cash.getReasonCodes", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_CallsBridge_RecordMovement()
    {
        Assert.Contains("cash.recordMovement", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_Sends_DropMovementType()
    {
        Assert.Contains("movementType: 'Drop'", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_Includes_BridgeTransportScript()
    {
        Assert.Contains("<script src=\"pos-bridge-transport.js\"></script>", AssetHtml);
    }

    // ── Key UI hooks preserved ────────────────────────────────────────────────

    [Fact]
    public void CashControlScreen_Preserves_SwitchTab()
    {
        Assert.Contains("function switchTab", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_Preserves_NumKey()
    {
        Assert.Contains("function numKey", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_Preserves_ClearAmount()
    {
        Assert.Contains("function clearAmount", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_Preserves_SetAmount()
    {
        Assert.Contains("function setAmount", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_Preserves_SubmitAction()
    {
        Assert.Contains("function submitAction", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_Includes_EscapeHtml()
    {
        Assert.Contains("function escapeHtml", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_DoesNotLog_RawRecordMovementError()
    {
        Assert.DoesNotContain("console.error('[CashControl] Record movement failed:', err)", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_InjectionTab_IsDeferredAndDoesNotSubmit()
    {
        Assert.Contains("function switchTab", AssetHtml);
        Assert.Contains("tab === 'inject'", AssetHtml);
        Assert.Contains("Float Add / Cash In is not available yet.", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_AlertCodes_AreBound()
    {
        Assert.Contains("OVER_LIMIT", AssetHtml);
        Assert.Contains("SAFE_DROP_RECOMMENDED", AssetHtml);
    }

    [Fact]
    public void CashControlScreen_RecordMovement_UsesDropOnly()
    {
        Assert.Contains("movementType: 'Drop'", AssetHtml);
        Assert.DoesNotContain("movementType: 'Injection'", AssetHtml);
        Assert.DoesNotContain("movementType: 'Correction'", AssetHtml);
        Assert.DoesNotContain("movementType: 'Payout'", AssetHtml);
    }
}

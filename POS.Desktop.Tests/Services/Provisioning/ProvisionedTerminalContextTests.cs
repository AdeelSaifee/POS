using System;
using POS.Desktop.Services.Provisioning;
using Xunit;

namespace POS.Desktop.Tests.Services.Provisioning;

public class ProvisionedTerminalContextTests
{
    [Fact]
    public void UnprovisionedState_FailsClosed()
    {
        // Arrange & Act
        var context = new ProvisionedTerminalContext();

        // Assert
        Assert.False(context.IsProvisioned);
        Assert.Equal(0, context.CurrentTenantId);
        Assert.Equal(0, context.CurrentLocationId);
        Assert.Equal(0, context.CurrentTerminalId);
        Assert.True(context.CurrentRecord.IsUnprovisioned);
        Assert.False(context.CurrentRecord.IsFullyProvisioned);
        Assert.False(context.CurrentRecord.IsHalfProvisioned);
    }

    [Fact]
    public void ValidFullProvisioningRecord_ReturnsExpectedValues()
    {
        // Arrange
        var record = new ProvisioningRecord(TenantId: 42, LocationId: 101, TerminalId: 999);

        // Act
        var context = new ProvisionedTerminalContext(record);

        // Assert
        Assert.True(context.IsProvisioned);
        Assert.Equal(42, context.CurrentTenantId);
        Assert.Equal(101, context.CurrentLocationId);
        Assert.Equal(999, context.CurrentTerminalId);
        Assert.False(context.CurrentRecord.IsUnprovisioned);
        Assert.True(context.CurrentRecord.IsFullyProvisioned);
        Assert.False(context.CurrentRecord.IsHalfProvisioned);
    }

    [Theory]
    [InlineData(null, 101, 999)]
    [InlineData(42, null, 999)]
    [InlineData(42, 101, null)]
    [InlineData(0, 101, 999)]
    [InlineData(42, -1, 999)]
    [InlineData(42, 101, 0)]
    public void PartialOrHalfProvisionedRecord_FailsClosed(int? tenantId, int? locationId, int? terminalId)
    {
        // Arrange
        var record = new ProvisioningRecord(tenantId, locationId, terminalId);

        // Act
        var context = new ProvisionedTerminalContext(record);

        // Assert
        Assert.False(context.IsProvisioned);
        Assert.Equal(0, context.CurrentTenantId);
        Assert.Equal(0, context.CurrentLocationId);
        Assert.Equal(0, context.CurrentTerminalId);
        Assert.False(context.CurrentRecord.IsUnprovisioned);
        Assert.False(context.CurrentRecord.IsFullyProvisioned);
        Assert.True(context.CurrentRecord.IsHalfProvisioned);
    }

    [Fact]
    public void ContextValues_RemainStableAndConsistent_AfterInitialization()
    {
        // Arrange
        var record = new ProvisioningRecord(TenantId: 10, LocationId: 20, TerminalId: 30);
        var context = new ProvisionedTerminalContext(record);

        // Act & Assert
        Assert.True(context.IsProvisioned);
        Assert.Equal(10, context.CurrentTenantId);
        Assert.Equal(20, context.CurrentLocationId);
        Assert.Equal(30, context.CurrentTerminalId);

        // Update to unprovisioned state
        context.UpdateState(ProvisioningRecord.Unprovisioned);
        Assert.False(context.IsProvisioned);
        Assert.Equal(0, context.CurrentTenantId);
        Assert.Equal(0, context.CurrentLocationId);
        Assert.Equal(0, context.CurrentTerminalId);
    }

    [Fact]
    public void NoUnsafeDefaultValues_AreExposed()
    {
        // Arrange
        var context = new ProvisionedTerminalContext();

        // Assert that the default values are exactly 0 (fail-closed) and not some fallback default tenant ID
        Assert.Equal(0, context.CurrentTenantId);
        Assert.Equal(0, context.CurrentLocationId);
        Assert.Equal(0, context.CurrentTerminalId);
    }

    [Fact]
    public void UpdateState_WithNullRecord_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new ProvisionedTerminalContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => context.UpdateState(null!));
    }
}

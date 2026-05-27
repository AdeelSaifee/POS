using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using POS.Desktop.Configuration;
using POS.Desktop.Services.Provisioning;
using POS.Shared.Contracts;
using Xunit;

namespace POS.Desktop.Tests.Services.Provisioning;

public class ProvisioningConfigTests
{
    [Fact]
    public void Load_WithNullConfiguration_ReturnsUnprovisioned()
    {
        // Act
        var record = ProvisioningConfigLoader.Load(null!);

        // Assert
        Assert.NotNull(record);
        Assert.True(record.IsUnprovisioned);
        Assert.False(record.IsFullyProvisioned);
    }

    [Fact]
    public void Load_WithMissingProvisioningSection_ReturnsUnprovisioned()
    {
        // Arrange
        var inMemoryConfig = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        // Act
        var record = ProvisioningConfigLoader.Load(configuration);

        // Assert
        Assert.NotNull(record);
        Assert.True(record.IsUnprovisioned);
        Assert.False(record.IsFullyProvisioned);
    }

    [Fact]
    public void Load_WithEmptyValues_ReturnsUnprovisioned()
    {
        // Arrange
        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Provisioning:TenantId", "" },
            { "Provisioning:LocationId", "   " },
            { "Provisioning:TerminalId", null }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        // Act
        var record = ProvisioningConfigLoader.Load(configuration);

        // Assert
        Assert.NotNull(record);
        Assert.True(record.IsUnprovisioned);
        Assert.False(record.IsFullyProvisioned);
    }

    [Fact]
    public void Load_WithValidConfig_ReturnsFullyProvisionedRecord()
    {
        // Arrange
        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Provisioning:TenantId", "42" },
            { "Provisioning:LocationId", "101" },
            { "Provisioning:TerminalId", "999" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        // Act
        var record = ProvisioningConfigLoader.Load(configuration);

        // Assert
        Assert.NotNull(record);
        Assert.True(record.IsFullyProvisioned);
        Assert.Equal(42, record.TenantId);
        Assert.Equal(101, record.LocationId);
        Assert.Equal(999, record.TerminalId);
    }

    [Theory]
    [InlineData("42", "101", "abc")]
    [InlineData("abc", "101", "999")]
    [InlineData("42", null, "999")]
    [InlineData("-1", "101", "999")]
    [InlineData("42", "0", "999")]
    public void Load_WithPartialOrInvalidConfig_ReturnsHalfProvisionedOrInvalidRecord(string? tenantId, string? locationId, string? terminalId)
    {
        // Arrange
        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Provisioning:TenantId", tenantId },
            { "Provisioning:LocationId", locationId },
            { "Provisioning:TerminalId", terminalId }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        // Act
        var record = ProvisioningConfigLoader.Load(configuration);

        // Assert
        Assert.NotNull(record);
        Assert.False(record.IsFullyProvisioned);
        Assert.False(record.IsUnprovisioned);
        Assert.True(record.IsHalfProvisioned);
    }

    [Fact]
    public void DesktopHostBuilder_Resolves_IProvisionedTerminalContext_As_ProvisionedTerminalContext()
    {
        // Arrange
        var host = DesktopHostBuilder.CreateHostBuilder(Array.Empty<string>()).Build();

        // Act
        var context = host.Services.GetService<IProvisionedTerminalContext>();

        // Assert
        Assert.NotNull(context);
        Assert.IsType<ProvisionedTerminalContext>(context);
    }

    [Fact]
    public void DesktopHostBuilder_ResolvesContextAsSingleton()
    {
        // Arrange
        var host = DesktopHostBuilder.CreateHostBuilder(Array.Empty<string>()).Build();

        // Act
        var context1 = host.Services.GetService<IProvisionedTerminalContext>();
        var context2 = host.Services.GetService<IProvisionedTerminalContext>();

        // Assert
        Assert.NotNull(context1);
        Assert.Same(context1, context2);
    }

    [Fact]
    public void NoProvisionedTerminalContext_IsStillAvailable()
    {
        // Act
        var context = new NoProvisionedTerminalContext();

        // Assert
        Assert.NotNull(context);
        Assert.False(context.IsProvisioned);
        Assert.Equal(0, context.CurrentTenantId);
        Assert.Equal(0, context.CurrentLocationId);
        Assert.Equal(0, context.CurrentTerminalId);
    }
}

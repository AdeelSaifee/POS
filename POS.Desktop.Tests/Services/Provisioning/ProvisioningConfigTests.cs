using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using POS.Desktop.Configuration;
using POS.Desktop.Data;
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

    [Fact]
    public void RegisteredContext_RemainsConsistentAcrossServiceScopes()
    {
        // Arrange
        var host = DesktopHostBuilder.CreateHostBuilder(Array.Empty<string>()).Build();

        // Act
        IProvisionedTerminalContext contextInScope1;
        IProvisionedTerminalContext contextInScope2;

        using (var scope1 = host.Services.CreateScope())
        {
            contextInScope1 = scope1.ServiceProvider.GetRequiredService<IProvisionedTerminalContext>();
        }

        using (var scope2 = host.Services.CreateScope())
        {
            contextInScope2 = scope2.ServiceProvider.GetRequiredService<IProvisionedTerminalContext>();
        }

        // Assert
        Assert.Same(contextInScope1, contextInScope2);

        var realContext = Assert.IsType<ProvisionedTerminalContext>(contextInScope1);
        realContext.UpdateState(new ProvisioningRecord(42, 101, 999));

        Assert.True(contextInScope2.IsProvisioned);
        Assert.Equal(42, contextInScope2.CurrentTenantId);
        Assert.Equal(101, contextInScope2.CurrentLocationId);
        Assert.Equal(999, contextInScope2.CurrentTerminalId);
    }

    [Fact]
    public void UnprovisionedContext_FailsClosedAcrossServiceScopes()
    {
        // Arrange
        var builder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>());
            })
            .ConfigureServices((hostContext, services) =>
            {
                var provisioningRecord = ProvisioningConfigLoader.Load(hostContext.Configuration);
                services.AddSingleton<IProvisionedTerminalContext>(new ProvisionedTerminalContext(provisioningRecord));
            });

        using var host = builder.Build();

        // Act & Assert
        for (int i = 0; i < 3; i++)
        {
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IProvisionedTerminalContext>();

            Assert.False(context.IsProvisioned);
            Assert.Equal(0, context.CurrentTenantId);
            Assert.Equal(0, context.CurrentLocationId);
            Assert.Equal(0, context.CurrentTerminalId);
        }
    }

    [Theory]
    [InlineData("42", "101", "abc")]
    [InlineData("abc", "101", "999")]
    [InlineData("42", null, "999")]
    [InlineData("-1", "101", "999")]
    [InlineData("42", "0", "999")]
    public void HalfProvisionedConfig_FailsClosedAcrossServiceScopes(string? tenantId, string? locationId, string? terminalId)
    {
        // Arrange
        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Provisioning:TenantId", tenantId },
            { "Provisioning:LocationId", locationId },
            { "Provisioning:TerminalId", terminalId }
        };

        var builder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(inMemoryConfig);
            })
            .ConfigureServices((hostContext, services) =>
            {
                var provisioningRecord = ProvisioningConfigLoader.Load(hostContext.Configuration);
                services.AddSingleton<IProvisionedTerminalContext>(new ProvisionedTerminalContext(provisioningRecord));
            });

        using var host = builder.Build();

        // Act & Assert
        for (int i = 0; i < 2; i++)
        {
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IProvisionedTerminalContext>();

            Assert.False(context.IsProvisioned);
            Assert.Equal(0, context.CurrentTenantId);
            Assert.Equal(0, context.CurrentLocationId);
            Assert.Equal(0, context.CurrentTerminalId);
        }
    }

    [Fact]
    public void DbContextScopes_ResolveWithSameProvisioningContextValues()
    {
        // Arrange
        var host = DesktopHostBuilder.CreateHostBuilder(Array.Empty<string>()).Build();

        var context = host.Services.GetRequiredService<IProvisionedTerminalContext>();
        var realContext = Assert.IsType<ProvisionedTerminalContext>(context);
        realContext.UpdateState(new ProvisioningRecord(42, 101, 999));

        // Act & Assert
        using (var scope1 = host.Services.CreateScope())
        using (var scope2 = host.Services.CreateScope())
        {
            var dbContext1 = scope1.ServiceProvider.GetRequiredService<PosLocalDbContext>();
            var dbContext2 = scope2.ServiceProvider.GetRequiredService<PosLocalDbContext>();

            Assert.NotSame(dbContext1, dbContext2);

            var context1 = scope1.ServiceProvider.GetRequiredService<IProvisionedTerminalContext>();
            var context2 = scope2.ServiceProvider.GetRequiredService<IProvisionedTerminalContext>();

            Assert.Same(context1, context2);
            Assert.Equal(42, context1.CurrentTenantId);
            Assert.Equal(101, context1.CurrentLocationId);
            Assert.Equal(999, context1.CurrentTerminalId);

            var field = typeof(PosLocalDbContext).GetField("_provisionedTerminalContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(field);

            var resolvedContext1 = field.GetValue(dbContext1);
            var resolvedContext2 = field.GetValue(dbContext2);

            Assert.Same(context, resolvedContext1);
            Assert.Same(context, resolvedContext2);
        }
    }
}

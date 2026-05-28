using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using POS.Desktop.Configuration;
using POS.Desktop.Services.Auth;
using Xunit;

namespace POS.Desktop.Tests.Configuration;

public class DesktopHostBuilderTests
{
    [Fact]
    public void CreateHostBuilder_RegistersLocalEmployeeAuthService_AsIAuthService()
    {
        // Arrange
        // Passing an empty string array as arguments for generic host initialization.
        using var host = DesktopHostBuilder.CreateHostBuilder(Array.Empty<string>()).Build();
        using var scope = host.Services.CreateScope();

        // Act
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        // Assert
        Assert.NotNull(authService);
        Assert.IsType<LocalEmployeeAuthService>(authService);
        Assert.IsNotType<StubAuthService>(authService);
    }
}

using POS.Api.Contracts;

namespace POS.Api.Application.Health;

public sealed class HealthStatusService : IHealthStatusService
{
    private readonly IWebHostEnvironment _environment;

    public HealthStatusService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public HealthStatusDto GetStatus()
    {
        return new HealthStatusDto(
            Application: "POS.Api",
            Status: "Healthy",
            TimestampUtc: DateTimeOffset.UtcNow,
            Environment: _environment.EnvironmentName);
    }
}

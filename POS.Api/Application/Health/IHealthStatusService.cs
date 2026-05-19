namespace POS.Api.Application.Health;

using POS.Api.Contracts;

public interface IHealthStatusService : IApplicationService
{
    HealthStatusDto GetStatus();
}

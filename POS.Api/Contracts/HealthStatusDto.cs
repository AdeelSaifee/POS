namespace POS.Api.Contracts;

public sealed record HealthStatusDto(
    string Application,
    string Status,
    DateTimeOffset TimestampUtc,
    string Environment);

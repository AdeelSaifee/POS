using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace POS.Desktop.Services.Auth;

public sealed class StubAuthService : IAuthService
{
    private readonly ILogger<StubAuthService> _logger;

    private static readonly Dictionary<string, (string Name, string Role, string Pin)> _stubOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        { "OP001", ("Adeel Saifee", "Sr. Cashier", "1111") },
        { "OP002", ("Ahmed Khan", "Cashier", "2222") },
        { "OP003", ("Sara Ahmed", "Cashier", "3333") },
        { "OP004", ("Bilal Hassan", "Cashier", "4444") },
        { "MGR01", ("Zainab Malik", "Manager", "9999") },
        { "MGR02", ("Usman Ali", "Supervisor", "8888") }
    };

    public StubAuthService(ILogger<StubAuthService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<AuthResult> ValidatePinAsync(string operatorId, string pin, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(operatorId) || string.IsNullOrWhiteSpace(pin))
        {
            return Task.FromResult(new AuthResult(false, null));
        }

        if (_stubOperators.TryGetValue(operatorId, out var op) && op.Pin == pin)
        {
            _logger.LogInformation("Stub login successful for operator ID: {OperatorId}", operatorId);
            return Task.FromResult(new AuthResult(true, new StubOperatorDetails(operatorId, op.Name, op.Role)));
        }

        _logger.LogWarning("Stub login failed for operator ID: {OperatorId}", operatorId);
        return Task.FromResult(new AuthResult(false, null));
    }
}

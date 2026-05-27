using System.Threading;
using System.Threading.Tasks;

namespace POS.Desktop.Services.Auth;

public interface IAuthService
{
    Task<AuthResult> ValidatePinAsync(string operatorId, string pin, CancellationToken cancellationToken = default);
    Task<AuthResult> ValidateManagerPinAsync(string operatorId, string pin, CancellationToken cancellationToken = default);
}

public sealed record AuthResult(
    bool IsValid,
    OperatorDetails? Operator = null,
    string? ErrorCode = null);

public sealed record OperatorDetails(
    string OperatorId,
    string DisplayName,
    string Role,
    string? PermissionSetCode = null,
    int? LocationId = null,
    bool MustChangePin = false);

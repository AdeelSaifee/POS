using System.Threading;
using System.Threading.Tasks;

namespace POS.Desktop.Services.Auth;

public interface IAuthService
{
    Task<AuthResult> ValidatePinAsync(string operatorId, string pin, CancellationToken cancellationToken = default);
}

public sealed record AuthResult(bool IsValid, StubOperatorDetails? Operator);

public sealed record StubOperatorDetails(
    string OperatorId,
    string DisplayName,
    string Role);

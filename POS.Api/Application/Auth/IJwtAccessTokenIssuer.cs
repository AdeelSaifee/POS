using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("POS.Tests")]

namespace POS.Api.Application.Auth;

internal interface IJwtAccessTokenIssuer : IAuthApplicationService
{
    IssuedAccessToken IssueAccessToken(TokenPrincipalDescriptor descriptor);
}

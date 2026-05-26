using System;
using Microsoft.Extensions.Logging.Abstractions;
using POS.Desktop.Services.Session;
using Xunit;

namespace POS.Desktop.Tests.Services.Session;

public class OperatorSessionServiceTests
{
    private readonly OperatorSessionService _sessionService;

    public OperatorSessionServiceTests()
    {
        // Use NullLogger<OperatorSessionService> to avoid testing logger side-effects or requiring a mock/setup.
        _sessionService = new OperatorSessionService(NullLogger<OperatorSessionService>.Instance);
    }

    [Fact]
    public void New_OperatorSessionService_StartsInactive()
    {
        // Act & Assert
        Assert.False(_sessionService.IsActive);
    }

    [Fact]
    public void CurrentSession_IsNull_BeforeStartSession()
    {
        // Act & Assert
        Assert.Null(_sessionService.CurrentSession);
    }

    [Fact]
    public void StartSession_StoresSafeOperatorSessionSnapshot()
    {
        // Arrange
        var session = new OperatorSession(
            OperatorId: "op-123",
            DisplayName: "Jane Doe",
            Role: "Manager",
            LoginTime: DateTimeOffset.UtcNow.AddMinutes(-5),
            TerminalId: "term-99",
            SessionId: "sess-abc"
        );

        // Act
        _sessionService.StartSession(session);

        // Assert
        Assert.NotNull(_sessionService.CurrentSession);
        Assert.Same(session, _sessionService.CurrentSession);
    }

    [Fact]
    public void StartSession_MakesIsActiveTrue()
    {
        // Arrange
        var session = new OperatorSession(
            OperatorId: "op-123",
            DisplayName: "Jane Doe",
            Role: "Manager",
            LoginTime: DateTimeOffset.UtcNow
        );

        // Act
        _sessionService.StartSession(session);

        // Assert
        Assert.True(_sessionService.IsActive);
    }

    [Fact]
    public void StartSession_PreservesAllRequiredFields()
    {
        // Arrange
        var operatorId = "op-123";
        var displayName = "Jane Doe";
        var role = "Manager";
        var loginTime = new DateTimeOffset(2026, 5, 27, 3, 0, 0, TimeSpan.Zero);
        var terminalId = "term-99";
        var sessionId = "sess-abc";

        var session = new OperatorSession(
            OperatorId: operatorId,
            DisplayName: displayName,
            Role: role,
            LoginTime: loginTime,
            TerminalId: terminalId,
            SessionId: sessionId
        );

        // Act
        _sessionService.StartSession(session);
        var current = _sessionService.CurrentSession;

        // Assert
        Assert.NotNull(current);
        Assert.Equal(operatorId, current.OperatorId);
        Assert.Equal(displayName, current.DisplayName);
        Assert.Equal(role, current.Role);
        Assert.Equal(loginTime, current.LoginTime);
        Assert.Equal(terminalId, current.TerminalId);
        Assert.Equal(sessionId, current.SessionId);
    }

    [Fact]
    public void ClearSession_RemovesCurrentSession()
    {
        // Arrange
        var session = new OperatorSession(
            OperatorId: "op-123",
            DisplayName: "Jane Doe",
            Role: "Manager",
            LoginTime: DateTimeOffset.UtcNow
        );
        _sessionService.StartSession(session);

        // Act
        _sessionService.ClearSession();

        // Assert
        Assert.Null(_sessionService.CurrentSession);
    }

    [Fact]
    public void ClearSession_MakesIsActiveFalse()
    {
        // Arrange
        var session = new OperatorSession(
            OperatorId: "op-123",
            DisplayName: "Jane Doe",
            Role: "Manager",
            LoginTime: DateTimeOffset.UtcNow
        );
        _sessionService.StartSession(session);

        // Act
        _sessionService.ClearSession();

        // Assert
        Assert.False(_sessionService.IsActive);
    }

    [Fact]
    public void ClearSession_IsSafeAndIdempotent_WhenNoSessionExists()
    {
        // Act & Assert
        // First clear on inactive service
        var ex = Record.Exception(() => _sessionService.ClearSession());
        Assert.Null(ex);
        Assert.False(_sessionService.IsActive);
        Assert.Null(_sessionService.CurrentSession);

        // Populate and clear twice
        var session = new OperatorSession("op-123", "Jane Doe", "Manager", DateTimeOffset.UtcNow);
        _sessionService.StartSession(session);

        _sessionService.ClearSession();
        var ex2 = Record.Exception(() => _sessionService.ClearSession());
        Assert.Null(ex2);
        Assert.False(_sessionService.IsActive);
        Assert.Null(_sessionService.CurrentSession);
    }

    [Fact]
    public void StartSession_RejectsNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sessionService.StartSession(null!));
    }
}

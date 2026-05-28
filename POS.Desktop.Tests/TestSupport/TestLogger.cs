using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace POS.Desktop.Tests.TestSupport;

/// <summary>
/// Minimal test-only logger that collects logged messages to support assertions in tests.
/// </summary>
public sealed class TestLogger<T> : ILogger<T>
{
    public List<string> LoggedMessages { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        LoggedMessages.Add(message);
        if (exception != null)
        {
            LoggedMessages.Add(exception.ToString());
        }
    }
}

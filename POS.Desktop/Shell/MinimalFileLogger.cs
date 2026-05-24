using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace POS.Desktop.Shell;

/// <summary>
/// A extremely minimal file logger for shell-level diagnostics.
/// </summary>
public sealed class MinimalFileLoggerProvider : ILoggerProvider
{
    private readonly string _fullPath;

    public MinimalFileLoggerProvider(string fullPath)
    {
        _fullPath = fullPath;
    }

    public ILogger CreateLogger(string categoryName) => new MinimalFileLogger(_fullPath, categoryName);

    public void Dispose() { }

    private sealed class MinimalFileLogger : ILogger
    {
        private static readonly object _lock = new();
        private readonly string _fullPath;
        private readonly string _categoryName;

        public MinimalFileLogger(string fullPath, string categoryName)
        {
            _fullPath = fullPath;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] [{_categoryName}] {message}";

            if (exception != null)
            {
                logLine += Environment.NewLine + exception.ToString();
            }

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_fullPath, logLine + Environment.NewLine);
                }
                catch
                {
                    // Fail silently to prevent crashing the app due to logging issues
                }
            }
        }
    }
}

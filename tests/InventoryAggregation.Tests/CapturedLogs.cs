using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace InventoryAggregation.Tests;

/// <summary>A logging provider that keeps every entry the app writes, so a test can check what was logged.</summary>
internal sealed class CapturedLogs : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> entries = new();

    public IReadOnlyCollection<LogEntry> Entries => entries;

    public ILogger CreateLogger(string categoryName) => new Logger(loggerName: categoryName, entries);

    public void Dispose()
    {
    }

    private sealed class Logger(string loggerName, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            entries.Enqueue(new LogEntry(loggerName, logLevel, formatter(state, exception), exception));
    }
}

/// <summary>One captured log entry. The logger name and message make a failing test's output readable.</summary>
internal sealed record LogEntry(string LoggerName, LogLevel Level, string Message, Exception? Exception);

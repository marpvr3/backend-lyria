using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Lyria.Infrastructure.IntegrationTests;

/// <summary>
/// Logger en memoria para verificar los mensajes emitidos durante las pruebas.
/// </summary>
internal sealed class RecordingLogger : ILogger
{
    private readonly List<RecordedLog> _entries = [];

    public IReadOnlyList<RecordedLog> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        _entries.Add(new RecordedLog(
            logLevel,
            eventId,
            formatter(state, exception),
            exception));
    }

    public bool ContainsMessage(string fragment) =>
        _entries.Exists(entry =>
            entry.Message.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    public override string ToString() =>
        string.Join(
            Environment.NewLine,
            _entries.Select(entry => string.Format(
                CultureInfo.InvariantCulture,
                "[{0}] {1}",
                entry.Level,
                entry.Message)));
}

internal sealed record RecordedLog(
    LogLevel Level,
    EventId EventId,
    string Message,
    Exception? Exception);

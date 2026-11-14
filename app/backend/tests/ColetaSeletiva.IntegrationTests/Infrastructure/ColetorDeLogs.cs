using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ColetaSeletiva.IntegrationTests.Infrastructure;

internal sealed class ColetorDeLogs : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _mensagens = new();

    public IReadOnlyList<string> ObterMensagens()
    {
        return _mensagens.ToArray();
    }

    public void Limpar()
    {
        while (_mensagens.TryDequeue(out _))
        {
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new Logger(this, categoryName);
    }

    public void Dispose()
    {
    }

    private sealed class Logger : ILogger
    {
        private readonly ColetorDeLogs _coletor;
        private readonly string _categoria;

        public Logger(ColetorDeLogs coletor, string categoria)
        {
            _coletor = coletor;
            _categoria = categoria;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            var mensagem = formatter(state, exception);
            if (exception is not null)
            {
                mensagem = $"{mensagem} | {exception}";
            }

            _coletor._mensagens.Enqueue($"{_categoria}|{logLevel}|{mensagem}");
        }
    }
}

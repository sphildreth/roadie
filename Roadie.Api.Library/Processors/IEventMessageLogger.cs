using Microsoft.Extensions.Logging;
using System;

namespace Roadie.Library.Processors
{
    public interface IEventMessageLogger
    {
        event EventHandler<EventMessage> Messages;

        IDisposable BeginScope<TState>(TState state);

        bool IsEnabled(LogLevel logLevel);

        void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter);
    }
}
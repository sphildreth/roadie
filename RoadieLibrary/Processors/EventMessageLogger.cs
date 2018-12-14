using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Processors
{
    public class EventMessageLogger : ILogger, IEventMessageLogger
    {
        public event EventHandler<EventMessage> Messages;

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Messages?.Invoke(this, new EventMessage { Level = logLevel, Message = formatter(state, exception) });
        }

        private void OnEventMessage(EventMessage message)
        {
            Messages?.Invoke(this, message);
        }

        
    }
}

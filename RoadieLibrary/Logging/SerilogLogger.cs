using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Logging
{
    public sealed class Logger : ILogger
    {
        private Serilog.Core.Logger _log = null;

        public string Name
        {
            get
            {
                return "Roadie Serilog Logger";
            }
        }

        public Logger()
        {
            this._log = new LoggerConfiguration()
                            .MinimumLevel.Verbose()
                            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
                            .WriteTo.File("logs//log-.txt", Serilog.Events.LogEventLevel.Debug)
                            .WriteTo.File("logs//errors-.txt", Serilog.Events.LogEventLevel.Error)
                            .CreateLogger();
        }


        public void Debug(string message)
        {
            this._log.Debug(message);
        }

        public void Debug(string message, params object[] args)
        {
            this._log.Debug(message, args);
        }

        public void Error(string message)
        {
            this._log.Error(message);
        }

        public void Error(string message, params object[] args)
        {
            this._log.Error(message, args);
        }

        public void Error(Exception exception, string message = null, bool isStackTraceIncluded = true)
        {
            this._log.Error(exception, message);
        }

        public void Fatal(string message)
        {
            this._log.Fatal(message);
        }

        public void Fatal(string message, params object[] args)
        {
            this._log.Fatal(message, args);
        }

        public void Fatal(Exception exception, string message = null, bool isStackTraceIncluded = true)
        {
            this._log.Fatal(exception, message);
        }

        public void Info(string message)
        {
            this._log.Information(message);
        }

        public void Info(string message, params object[] args)
        {
            this._log.Information(message, args);
        }

        public void Trace(string message)
        {
            this._log.Verbose(message);
        }

        public void Trace(string message, params object[] args)
        {
            this._log.Verbose(message, args);
        }

        public void Warning(string message)
        {
            this._log.Warning(message);
        }

        public void Warning(string message, params object[] args)
        {
            this._log.Warning(message, args);
        }
    }
}

using Serilog;
using System.Diagnostics;

namespace Roadie.Api
{
    public class LoggingTraceListener : TraceListener
    {
        public override void Write(string message) => WriteLog(message);

        public override void WriteLine(string message) => WriteLog(message);

        public override void WriteLine(string o, string category) => WriteLog(o, category);

        public override void Write(string o, string category) => WriteLog(o, category);

        private void WriteLog(string message, string category = null)
        {
            switch (category?.ToLower())
            {
                case "warning":
                    Log.Warning(message);
                    break;

                case "error":
                    Log.Error(message);
                    break;

                default:
                    Log.Verbose(message);
                    break;
            }
        }
    }
}

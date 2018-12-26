using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;

namespace Roadie.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                            .ReadFrom.Configuration(Configuration)
                            .CreateLogger();

            try
            {
                Log.Information("Starting web host");

                Trace.Listeners.Add(new LoggingTraceListener());

#if DEBUG
                // Logging Output tests
                Log.Verbose(":: Log Test: Verbose (Trace,None)");                           // Microsoft.Extensions.Logging.LogLevel.Trace and Microsoft.Extensions.Logging.LogLevel.None
                Log.Debug(":: Log Test: Debug");                                            // Microsoft.Extensions.Logging.LogLevel.Debug 
                Log.Information(":: Log Test: Information");                                // Microsoft.Extensions.Logging.LogLevel.Information
                Log.Warning(":: Log Test: Warning");                                        // Microsoft.Extensions.Logging.LogLevel.Warning
                Log.Error(new Exception("Log Test Exception"), "Log Test Error Message");   // Microsoft.Extensions.Logging.LogLevel.Error
                Log.Fatal(":: Log Test: Fatal (Critial)");                                  // Microsoft.Extensions.Logging.LogLevel.Critical
                Trace.WriteLine(":: Log Test: Trace WriteLine()");
#endif
                CreateWebHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseSerilog();

        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                   .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                   .AddEnvironmentVariables()
                   .Build();
    }

    public class LoggingTraceListener : TraceListener
    {
        public override void Write(string message)
        {
            Log.Verbose(message);
        }

        public override void WriteLine(string message)
        {
            Log.Verbose(message);
        }
    }
}
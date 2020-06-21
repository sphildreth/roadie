using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Pastel;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;

namespace Roadie.Api
{
    public class Program
    {
        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
            .AddEnvironmentVariables()
            .Build();

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
                Log.Verbose(":: Log Test: Verbose (Trace,None), Test 'value', Test: Value"); // Microsoft.Extensions.Logging.LogLevel.Trace and Microsoft.Extensions.Logging.LogLevel.None
                Log.Debug(":: Log Test: Debug, Test 'value', Test: Value"); // Microsoft.Extensions.Logging.LogLevel.Debug
                Log.Information(":: Log Test: Information, Test 'value', Test: Value"); // Microsoft.Extensions.Logging.LogLevel.Information
                Log.Warning(":: Log Test: Warning, Test 'value', Test: Value"); // Microsoft.Extensions.Logging.LogLevel.Warning
                Log.Error(new Exception("Log Test Exception"), "Log Test Error Message, Test 'value', Test: Value"); // Microsoft.Extensions.Logging.LogLevel.Error
                Log.Fatal(":: Log Test: Fatal (Critial), Test 'value', Test: Value"); // Microsoft.Extensions.Logging.LogLevel.Critical
                Trace.WriteLine(":: Log Test: Trace WriteLine(), Test 'value', Test: Value");
#endif
                Console.WriteLine("");
                Console.WriteLine(@" ____   __    __   ____  __  ____     __   ____  __  ".Pastel("#FEFF0E"));
                Console.WriteLine(@"(  _ \ /  \  / _\ (    \(  )(  __)   / _\ (  _ \(  ) ".Pastel("#F49014"));
                Console.WriteLine(@" )   /(  O )/    \ ) D ( )(  ) _)   /    \ ) __/ )(  ".Pastel("#E30014"));
                Console.WriteLine(@"(__\_) \__/ \_/\_/(____/(__)(____)  \_/\_/(__)  (__) ".Pastel("#DB0083"));
                Console.WriteLine("");

                CreateHostBuilder(args).Build().Run();
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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
             Host.CreateDefaultBuilder(args)
                 .ConfigureWebHostDefaults(webBuilder =>
                 {
                     webBuilder.UseStartup<Startup>();
                 })
                 .UseSerilog();
    }
}
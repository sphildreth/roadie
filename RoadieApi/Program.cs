using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Roadie.Api
{
    public class Program
    {

        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();


        //public static void Main(string[] args)
        //{
        //    var config = new ConfigurationBuilder()
        //            .SetBasePath(Directory.GetCurrentDirectory())
        //            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        //            .AddJsonFile("appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
        //            .AddCommandLine(args)
        //            .Build();

        //    var host = new WebHostBuilder()
        //            .UseKestrel()
        //            .UseConfiguration(config)
        //            .UseContentRoot(Directory.GetCurrentDirectory())
        //            .ConfigureLogging((hostingContext, logging) =>
        //            {
        //                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
        //            })
        //            .UseStartup<Startup>()
        //            .Build();

        //    host.Run();

        //}
    }
}

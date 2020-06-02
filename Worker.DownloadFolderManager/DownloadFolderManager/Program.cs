using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Serilog;

namespace DownloadFolderManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().
                MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
#if DEBUG
         .WriteTo.Console() 
#endif
                .WriteTo.File(@"c:\temp\logs\DownloadFolderManager.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting up service");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex,"Cant start service");
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var result = Host.CreateDefaultBuilder(args).
                UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                });
            // set log


            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                result.UseSystemd();
            }
            else
            {
                result.UseWindowsService();
            }
            return result;
        }


    }
}

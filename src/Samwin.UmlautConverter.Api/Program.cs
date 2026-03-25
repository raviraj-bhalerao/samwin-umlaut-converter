using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using System.Threading.Tasks;

namespace Samwin.UmlautConverter.Api
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static async Task Main(string[] args)
        {

            var activitySource = new ActivitySource("Samwin.UmlautConverter");
            IHost hostToRun;
            using (var activity = activitySource.StartActivity("AppStartup"))
            {
                SetEnvironmentVariables();
                // NLog: setup the logger first to catch all errors
                var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();

                try
                {
                    logger.Debug("init main");
                    hostToRun = CreateHostBuilder(args).Build();
                }
                catch (Exception exception)
                {
                    // NLog: catch setup errors
                    logger.Error(exception, "Stopped program because of exception");
                    throw;
                }
                finally
                {
                    SetEnvironmentVariables(true);
                    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                    LogManager.Flush();
                }
            }
            if(hostToRun != null)
            {
                await hostToRun.RunAsync();
            }
        }

        private static void SetEnvironmentVariables(bool reset = false)
        {
            AppContext.SetSwitch("System.Diagnostics.DiagnosticSource.Logging", true);
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS", "true");
#if DEBUG
            if (File.Exists("env.tmp"))
            {
                var json = File.ReadAllText("env.tmp");
                var envVars = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (envVars != null)
                {
                    foreach (var kv in envVars)
                        Environment.SetEnvironmentVariable(kv.Key, (reset ? null : kv.Value));
                }
            }
#endif
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureLogging(logging => logging.ClearProviders());
                })
                .UseNLog();
    }
}
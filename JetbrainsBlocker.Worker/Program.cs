using JetbrainsBlocker.Worker.Config;
using JetbrainsBlocker.Worker.Helpers;
using JetbrainsBlocker.Worker.Options;
using JetbrainsBlocker.Worker.Service;
using Serilog;
using Serilog.Events;

const string ServiceName = "JetbrainsBlocker";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.EventLog(ServiceName, manageEventSource: true)
    .CreateBootstrapLogger();

if (!OsHelper.IsHostOsWindows())
{
    Log.Fatal("This application work on windows only");
    await Log.CloseAndFlushAsync();
    Environment.Exit(1);
}

#pragma warning disable CA1416
#if !DEBUG
if (!OsHelper.HasAdminRole())
{
    Log.Fatal("This application manages firewall rules, therefore must be run as administrator");
    await Log.CloseAndFlushAsync();
    Environment.Exit(1);
}
#endif

try
{
    var cfg = new ConfigManager();
    if (!cfg.IsConfigFileExist())
    {
        await cfg.WriteDefaultsAsync();
    }

    var builder = Host.CreateApplicationBuilder(args);

    builder.Configuration.AddJsonFile(cfg.ConfigFilePath, false, true);
    builder.Services
        .AddWindowsService(options => options.ServiceName = ServiceName)
        .AddSerilog(
            (services, configuration) => configuration
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.EventLog(ServiceName, manageEventSource: true))
        .Configure<ServiceOptions>(builder.Configuration.GetSection(ServiceOptions.SectionName))
        .AddSingleton<BlockerService>()
        .AddSingleton<FirewallService>()
        .AddHostedService<BlockerHostedService>();

    var host = builder.Build();
    host.Run();
}
catch (Exception e)
{
    Log.Fatal(e, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

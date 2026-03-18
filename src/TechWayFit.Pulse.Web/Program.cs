using Serilog;
using Serilog.Events;
using TechWayFit.Pulse.Infrastructure.Extensions;
using TechWayFit.Pulse.Web.Extensions;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: Path.Combine("App_Data", "logs", "pulse-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting TechWayFit Pulse application");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddUserSecrets<Program>();
    }

    builder.Services
        .AddPulseOptions(builder.Configuration)
        .AddPulseAuthentication(builder.Environment)
        .AddPulseWebServices(builder.Configuration, builder.Environment)
        .AddPulseAIServices(builder.Configuration, builder.Environment)
        .AddPulseApplicationServices(builder.Configuration)
        .AddPulseSignalR(builder.Configuration)
        .AddPulseHealthChecks();

    var app = builder.Build();

    app.Services.EnsurePulseDatabase(builder.Configuration);

    app.UsePulseWebPipeline();
    app.MapPulseEndpoints();

    Log.Information("TechWayFit Pulse application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

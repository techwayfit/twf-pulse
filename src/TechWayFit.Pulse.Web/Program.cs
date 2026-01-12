using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Application.Services;
using TechWayFit.Pulse.Infrastructure.Persistence;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;
using TechWayFit.Pulse.Web.Data;
using TechWayFit.Pulse.Web.Api;
using TechWayFit.Pulse.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });

builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddSingleton<IFacilitatorTokenStore, FacilitatorTokenStore>();

// Add HttpClient for API service with dynamic base URL
builder.Services.AddHttpClient<IPulseApiService, PulseApiService>((serviceProvider, client) =>
{
    var httpContext = serviceProvider.GetService<IHttpContextAccessor>()?.HttpContext;
    if (httpContext != null)
    {
        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
 client.BaseAddress = new Uri(baseUrl);
    }
  else
    {
  // Fallback for command line scenarios
        client.BaseAddress = new Uri("https://localhost:7100");
    }
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add HttpContextAccessor for dynamic URL resolution
builder.Services.AddHttpContextAccessor();

var useInMemory = builder.Configuration.GetValue<bool>("Pulse:UseInMemory");
var connectionString = builder.Configuration.GetConnectionString("PulseDb");
builder.Services.AddDbContext<PulseDbContext>(options =>
{
    if (useInMemory || string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseInMemoryDatabase("Pulse");
        return;
    }

    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<IParticipantRepository, ParticipantRepository>();
builder.Services.AddScoped<IResponseRepository, ResponseRepository>();
builder.Services.AddScoped<IContributionCounterRepository, ContributionCounterRepository>();

builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IParticipantService, ParticipantService>();
builder.Services.AddScoped<IResponseService, ResponseService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddSignalR();

var app = builder.Build();

// Ensure database is created for SQLite
if (!useInMemory)
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        dbContext.Database.EnsureCreated();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Important: UseHttpsRedirection should come before UseStaticFiles
app.UseHttpsRedirection();

// Configure static files with proper options
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Add cache headers for better performance
    if (ctx.File.Name.EndsWith(".css") || ctx.File.Name.EndsWith(".js"))
   {
     ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=600");
        }
    }
});

app.UseRouting();

// Map API controllers first (higher priority)
app.MapControllers();

// Map SignalR hub
app.MapHub<TechWayFit.Pulse.Web.Hubs.WorkshopHub>("/hubs/workshop");

// Map Blazor Hub
app.MapBlazorHub();

// Map specific MVC routes
app.MapControllerRoute(
    name: "ui",
    pattern: "ui/{action=Index}/{id?}",
    defaults: new { controller = "Ui" });

// Map fallback to Blazor only for root and non-API routes
app.MapFallbackToPage("/_Host");

app.Run();

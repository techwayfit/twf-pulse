using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
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

builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
// Add authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
 {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.Name = "TechWayFit.Pulse.Auth";
  options.Cookie.HttpOnly = true;
  options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
 options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

// Add Data Protection with custom file-based key storage
var keysPath = Path.Combine(builder.Environment.ContentRootPath, "keys");
var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
var customRepo = new CustomFileSystemXmlRepository(
    new DirectoryInfo(keysPath),
    loggerFactory.CreateLogger<CustomFileSystemXmlRepository>());

builder.Services.AddSingleton<IXmlRepository>(customRepo);
builder.Services.AddDataProtection()
    .SetApplicationName("TechWayFit.Pulse")
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews()
 .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });

// Add Blazor Server for interactive pages only
builder.Services.AddServerSideBlazor(options =>
{
 // Optimize Blazor Server for workshop scenarios
    options.DetailedErrors = builder.Environment.IsDevelopment();
    options.DisconnectedCircuitMaxRetained = 10; // Limit retained circuits
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3); // Reduce retention time
    options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(30);
    options.MaxBufferedUnacknowledgedRenderBatches = 10; // Reduce memory usage
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
builder.Services.AddScoped<IFacilitatorUserRepository, FacilitatorUserRepository>();
builder.Services.AddScoped<ILoginOtpRepository, LoginOtpRepository>();

builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IParticipantService, ParticipantService>();
builder.Services.AddScoped<IResponseService, ResponseService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISessionCodeGenerator, SessionCodeGenerator>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Register email service based on configuration
var emailProvider = builder.Configuration.GetValue<string>("Email:Provider");
if (emailProvider?.Equals("Smtp", StringComparison.OrdinalIgnoreCase) == true)
{
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}
else
{
    // Default to console for development/testing
    builder.Services.AddScoped<IEmailService, ConsoleEmailService>();
}

// SignalR for real-time features
builder.Services.AddSignalR(options =>
{
    // Optimize SignalR for workshop scenarios
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB limit
});

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

// Configure static files with proper caching
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

// Add authentication & authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map API controllers first (highest priority)
app.MapControllers();

// Map SignalR hub for real-time features
app.MapHub<TechWayFit.Pulse.Web.Hubs.WorkshopHub>("/hubs/workshop");

// Map Blazor Hub (only for interactive pages)
app.MapBlazorHub();

// Map MVC routes for static pages (no WebSocket)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map Razor Pages (for Blazor interactive components only)
app.MapRazorPages();

// Blazor pages fallback (only for routes that need interactivity)
app.MapFallbackToPage("/_Host");

app.Run();

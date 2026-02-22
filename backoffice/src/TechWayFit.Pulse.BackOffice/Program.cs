using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TechWayFit.Pulse.BackOffice.Authorization;
using TechWayFit.Pulse.BackOffice.Core;
using TechWayFit.Pulse.BackOffice.Core.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ── Local overrides (highest priority, gitignored) ────────────────────────────
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

// ── Serilog ───────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/backoffice-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30));

// ── MVC ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── BackOffice Core services + DbContext ──────────────────────────────────────
builder.Services.AddBackOfficeCore(builder.Configuration);

// ── Authentication ────────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name        = "TechWayFit.Pulse.BackOffice.Auth";
        options.Cookie.HttpOnly    = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite    = SameSiteMode.Strict;
        options.LoginPath          = "/auth/login";
        options.LogoutPath         = "/auth/logout";
        options.AccessDeniedPath   = "/auth/access-denied";
        options.SlidingExpiration  = true;
        options.ExpireTimeSpan     = TimeSpan.FromHours(8);
    });

// ── Authorization policies ────────────────────────────────────────────────────
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy(PolicyNames.OperatorOrAbove, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Operator", "SuperAdmin"));

    opts.AddPolicy(PolicyNames.SuperAdminOnly, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("SuperAdmin"));
});

// ── Rate limiting (login protection) ─────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", o =>
    {
        o.PermitLimit      = 5;
        o.Window           = TimeSpan.FromMinutes(1);
        o.QueueLimit       = 0;
    });
});

// ── HTTP context accessor (for capturing IP in controllers) ───────────────────
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// ── Database initialise (SQLite only — SQL Server uses manual scripts) ──────────
await InitialiseDatabaseAsync(app);

app.Run();

static async Task InitialiseDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db          = scope.ServiceProvider.GetRequiredService<BackOfficeDbContext>();
    var logger      = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // For SQLite: auto-create BackOffice tables if they don’t exist yet.
        // For SQL Server: tables must be created via Scripts/v1.0/SqlServer/.
        if (db.Database.IsSqlite())
            await db.Database.EnsureCreatedAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialisation failed. Ensure BackOffice tables exist (see Scripts/v1.0/SqlServer/).");
    }
}

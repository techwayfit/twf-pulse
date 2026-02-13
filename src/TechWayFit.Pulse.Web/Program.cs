using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Application.Services;
using TechWayFit.Pulse.Infrastructure.Persistence;
using TechWayFit.Pulse.Infrastructure.Persistence.Repositories;
using TechWayFit.Pulse.Web.Data;
using TechWayFit.Pulse.Web.Api;
using TechWayFit.Pulse.Web.Services;
using TechWayFit.Pulse.Web.Configuration;
using TechWayFit.Pulse.Web.Handlers;

// Configure Serilog
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

    // Add Serilog to the application
    builder.Host.UseSerilog();

    builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

    // Explicitly load user secrets (needed for VS Code debugging)
    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddUserSecrets<Program>();
    }

    // Configure activity defaults
    builder.Services.Configure<ActivityDefaultsOptions>(
        builder.Configuration.GetSection(ActivityDefaultsOptions.SectionName));

    // Configure context document limits
    builder.Services.Configure<ContextDocumentLimitsOptions>(
        builder.Configuration.GetSection(ContextDocumentLimitsOptions.SectionName));
    
    // Configure AI quota limits
    builder.Services.Configure<AiQuotaOptions>(
        builder.Configuration.GetSection("AI:Quota"));

    // Add authentication services
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
     {
         options.LoginPath = "/account/login";
         options.LogoutPath = "/account/logout";
         options.AccessDeniedPath = "/account/login";
         options.ExpireTimeSpan = TimeSpan.FromHours(8); // 8 hours of inactivity
         options.SlidingExpiration = true; // Extends timeout on activity
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

    // Add session support for token storage
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromHours(2);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    // Add HttpContextAccessor for session access
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddSingleton<WeatherForecastService>();
    builder.Services.AddSingleton<IFacilitatorTokenStore, FacilitatorTokenStore>();
    builder.Services.AddSingleton<IParticipantTokenStore, ParticipantTokenStore>();

    // Register authentication cookie handler
    builder.Services.AddTransient<AuthenticationCookieHandler>();

    // Add HttpClient for API service with dynamic base URL and authentication cookie forwarding
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
    })
    .AddHttpMessageHandler<AuthenticationCookieHandler>();

    // Add default HttpClientFactory for dev/testing pages
    builder.Services.AddHttpClient();

    // Add named HttpClient for OpenAI (used by AI services) with retry policy
    builder.Services.AddHttpClient("openai", (client) =>
    {
        // BaseAddress may be overridden by configuration (full endpoint expected)
        var openAiBase = builder.Configuration["AI:OpenAI:Endpoint"];
        if (!string.IsNullOrWhiteSpace(openAiBase))
        {
            client.BaseAddress = new Uri(openAiBase);
            Log.Information("OpenAI HttpClient BaseAddress set to: {BaseAddress}", openAiBase);
        }
        else
        {
            Log.Warning("OpenAI endpoint not configured in settings");
        }
        client.Timeout = TimeSpan.FromSeconds(90); // Increased for retry attempts
    })
    .AddStandardResilienceHandler(options =>
    {
        // Configure retry with exponential backoff
        options.Retry.MaxRetryAttempts = 2;
        options.Retry.Delay = TimeSpan.FromSeconds(2);
        options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;

        // Configure circuit breaker
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120);
        options.CircuitBreaker.FailureRatio = 0.7;
        options.CircuitBreaker.MinimumThroughput = 5;

        // Configure timeout per attempt - increased for OpenAI
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);

        // Configure total timeout - increased for retries
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
    });

    // AI services: register real implementations when AI enabled and API key present, otherwise register mocks
    var aiEnabled = builder.Configuration.GetValue<bool>("AI:Enabled");
    var aiProvider = builder.Configuration.GetValue<string>("AI:Provider") ?? "Mock";
    var openAiApiKey = builder.Configuration["AI:OpenAI:ApiKey"];

    Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);
    Log.Information("AI Enabled: {Enabled}", aiEnabled);
    Log.Information("AI Provider: {Provider}", aiProvider);
    Log.Information("API Key present: {HasKey}, Length: {Length}",
        !string.IsNullOrWhiteSpace(openAiApiKey),
        openAiApiKey?.Length ?? 0);

    // Register AI services based on provider configuration
    // Providers: "OpenAI" (GPT AI), "MLNet" (ML.NET machine learning), "Intelligent" (NLP-based), "Mock" (simple)
    if (aiEnabled && !string.IsNullOrWhiteSpace(openAiApiKey) && aiProvider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
    {
        Log.Information("Registering REAL AI services (OpenAI API enabled)");
        builder.Services.AddScoped<TechWayFit.Pulse.Application.Abstractions.Services.IParticipantAIService, TechWayFit.Pulse.AI.Services.ParticipantAIService>();
        builder.Services.AddScoped<TechWayFit.Pulse.Application.Abstractions.Services.IFacilitatorAIService, TechWayFit.Pulse.AI.Services.FacilitatorAIService>();
        builder.Services.AddScoped<TechWayFit.Pulse.Application.Abstractions.Services.ISessionAIService, TechWayFit.Pulse.AI.Services.SessionAIService>();
    }
    else if (aiProvider.Equals("MLNet", StringComparison.OrdinalIgnoreCase))
    {
        Log.Information("Registering ML.NET AI services (Microsoft ML.NET machine learning)");
        builder.Services.AddScoped<TechWayFit.Pulse.Application.Abstractions.Services.IParticipantAIService, TechWayFit.Pulse.AI.Services.MockParticipantAIService>();
        builder.Services.AddScoped<TechWayFit.Pulse.Application.Abstractions.Services.IFacilitatorAIService, TechWayFit.Pulse.AI.Services.MockFacilitatorAIService>();
        builder.Services.AddScoped<TechWayFit.Pulse.Application.Abstractions.Services.ISessionAIService, TechWayFit.Pulse.AI.Services.MLNetSessionAIService>();
    }
    else if (aiProvider.Equals("Intelligent", StringComparison.OrdinalIgnoreCase))
    {
        Log.Information("Registering INTELLIGENT AI services (NLP-inspired keyword-based generation)");
        builder.Services.AddScoped<TechWayFit.Pulse.Application.Abstractions.Services.IParticipantAIService, TechWayFit.Pulse.AI.Services.MockParticipantAIService>();
        builder.Services.AddScoped<TechWayFit.Pulse.Application.Abstractions.Services.IFacilitatorAIService, TechWayFit.Pulse.AI.Services.MockFacilitatorAIService>();
        builder.Services.AddScoped<TechWayFit.Pulse.Application.Abstractions.Services.ISessionAIService, TechWayFit.Pulse.AI.Services.IntelligentSessionAIService>();
    }
    builder.Services.AddKeyedScoped<TechWayFit.Pulse.Application.Abstractions.Services.ISessionAIService, TechWayFit.Pulse.AI.Services.IntelligentSessionAIService>("Intelligent");
    

    // Register AI work queue and background processor
    builder.Services.AddSingleton<TechWayFit.Pulse.Application.Abstractions.Services.IAIWorkQueue, TechWayFit.Pulse.Infrastructure.AI.AIWorkQueue>();
    builder.Services.AddHostedService<TechWayFit.Pulse.Web.BackgroundServices.AIProcessingHostedService>();

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

        options.UseSqlite(connectionString, b => b.MigrationsAssembly("TechWayFit.Pulse.Web"));
    });

    builder.Services.AddScoped<ISessionRepository, SessionRepository>();
    builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
    builder.Services.AddScoped<IParticipantRepository, ParticipantRepository>();
    builder.Services.AddScoped<IResponseRepository, ResponseRepository>();
    builder.Services.AddScoped<IContributionCounterRepository, ContributionCounterRepository>();
    builder.Services.AddScoped<IFacilitatorUserRepository, FacilitatorUserRepository>();
    builder.Services.AddScoped<IFacilitatorUserDataRepository, FacilitatorUserDataRepository>();
    builder.Services.AddScoped<ILoginOtpRepository, LoginOtpRepository>();
    builder.Services.AddScoped<ISessionGroupRepository, SessionGroupRepository>();
    builder.Services.AddScoped<ISessionTemplateRepository, TechWayFit.Pulse.Infrastructure.Repositories.SessionTemplateRepository>();

    builder.Services.AddScoped<ISessionService, SessionService>();
    builder.Services.AddScoped<IActivityService, ActivityService>();
    builder.Services.AddScoped<IParticipantService, ParticipantService>();
    builder.Services.AddScoped<IResponseService, ResponseService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<IAiQuotaService, AiQuotaService>();
    builder.Services.AddScoped<IPollDashboardService, PollDashboardService>();
    builder.Services.AddScoped<IWordCloudDashboardService, WordCloudDashboardService>();
    builder.Services.AddScoped<IRatingDashboardService, RatingDashboardService>();
    builder.Services.AddScoped<IGeneralFeedbackDashboardService, GeneralFeedbackDashboardService>();
    builder.Services.AddScoped<ISessionCodeGenerator, SessionCodeGenerator>();
    builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
    builder.Services.AddScoped<ISessionGroupService, SessionGroupService>();
    builder.Services.AddScoped<ISessionTemplateService, TechWayFit.Pulse.Infrastructure.Services.SessionTemplateService>();

    // Background service for template initialization (non-blocking startup)
    builder.Services.AddHostedService<TechWayFit.Pulse.Web.BackgroundServices.TemplateInitializationHostedService>();

    // Token Services
    builder.Services.AddSingleton<IFacilitatorTokenService, FacilitatorTokenService>();
    builder.Services.AddScoped<IClientTokenService, ClientTokenService>();

    // Register file service and memory cache for template caching
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<IFileService, FileService>();

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

    // Handle status code errors (404, 403, etc.) - must be before UseStaticFiles
    app.UseStatusCodePagesWithReExecute("/Error/{0}");

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

    // Add Serilog request logging middleware
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = (httpContext, elapsed, ex) => ex != null
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode > 499
       ? LogEventLevel.Error
        : LogEventLevel.Information;
    });

    app.UseRouting();

    // Add session middleware for facilitator token storage
    app.UseSession();

    // Add authentication & authorization middleware
    app.UseAuthentication();

    // Add facilitator token middleware (after authentication, before authorization)
    app.UseMiddleware<TechWayFit.Pulse.Web.Middleware.FacilitatorTokenMiddleware>();
    
    // Add facilitator context middleware (populates AsyncLocal context)
    app.UseMiddleware<TechWayFit.Pulse.Web.Middleware.FacilitatorContextMiddleware>();

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

    // Note: Template initialization now happens in background via TemplateInitializationHostedService
    // This ensures fast app startup without blocking

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

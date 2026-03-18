using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Application.Services;
using TechWayFit.Pulse.Infrastructure.Extensions;
using TechWayFit.Pulse.Web.Activities;
using TechWayFit.Pulse.Web.Api;
using TechWayFit.Pulse.Web.Configuration;
using TechWayFit.Pulse.Web.HealthChecks;
using TechWayFit.Pulse.Web.Validation;
using TechWayFit.Pulse.Web.Services;

namespace TechWayFit.Pulse.Web.Extensions;

public static class PulseServiceCollectionExtensions
{
    public static IServiceCollection AddPulseOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ActivityDefaultsOptions>()
            .Bind(configuration.GetSection(ActivityDefaultsOptions.SectionName))
            .Validate(
                options => options.Poll.MaxResponsesPerParticipant > 0
                    && options.Rating.MaxResponsesPerParticipant > 0
                    && options.WordCloud.MaxSubmissionsPerParticipant > 0
                    && options.GeneralFeedback.MaxResponsesPerParticipant > 0,
                "Activity defaults must be greater than zero.")
            .ValidateOnStart();

        services.AddOptions<TechWayFit.Pulse.AI.Options.OpenAIOptions>()
            .Bind(configuration.GetSection(TechWayFit.Pulse.AI.Options.OpenAIOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Endpoint)
                    && !string.IsNullOrWhiteSpace(options.Model)
                    && options.MaxTokens > 0,
                "OpenAI options are invalid.")
            .ValidateOnStart();

        services.AddOptions<ContextDocumentLimitsOptions>()
            .Bind(configuration.GetSection(ContextDocumentLimitsOptions.SectionName))
            .Validate(
                options => options.SprintBacklogSummaryMaxChars > 0
                    && options.IncidentSummaryMaxChars > 0
                    && options.ProductSummaryMaxChars > 0,
                "Context document limits must be greater than zero.")
            .ValidateOnStart();

        services.AddOptions<AiQuotaOptions>()
            .Bind(configuration.GetSection(AiQuotaOptions.SectionName))
            .Validate(options => options.FreeSessionsPerMonth >= 0, "AI quota cannot be negative.")
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddPulseAuthentication(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/account/login";
                options.LogoutPath = "/account/logout";
                options.AccessDeniedPath = "/account/login";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Cookie.Name = "TechWayFit.Pulse.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

        services.AddAuthorization();

        var keysPath = Path.Combine(environment.ContentRootPath, "keys");
        var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
        var customRepo = new CustomFileSystemXmlRepository(
            new DirectoryInfo(keysPath),
            loggerFactory.CreateLogger<CustomFileSystemXmlRepository>());

        services.AddSingleton<IXmlRepository>(customRepo);
        services.AddDataProtection()
            .SetApplicationName("TechWayFit.Pulse")
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        return services;
    }

    public static IServiceCollection AddPulseWebServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? configuration["Pulse:Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "twf-pulse";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddRazorPages();
        services.AddControllersWithViews()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<CreateSessionRequestValidator>();

        services.AddServerSideBlazor(options =>
        {
            options.DetailedErrors = environment.IsDevelopment();
            options.DisconnectedCircuitMaxRetained = 10;
            options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
            options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(30);
            options.MaxBufferedUnacknowledgedRenderBatches = 10;
        });

        services.AddScoped<CircuitHandler, TechWayFit.Pulse.Web.Infrastructure.MobileCircuitHandler>();

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(2);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        services.AddHttpContextAccessor();

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/json",
                "application/javascript",
                "text/css",
                "text/html",
                "text/plain",
                "image/svg+xml"
            });
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = System.IO.Compression.CompressionLevel.Fastest;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = System.IO.Compression.CompressionLevel.Fastest;
        });

        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 100 * 1024 * 1024;
            options.CompactionPercentage = 0.25;
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    "{\"error\":\"rate_limited\",\"message\":\"Too many requests. Please try again shortly.\"}",
                    token);
            };

            options.AddPolicy("participant-join", httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: $"join:{httpContext.Connection.RemoteIpAddress}",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy("api-write", httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: $"api-write:{httpContext.Connection.RemoteIpAddress}",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy("participant-submit", httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: $"submit:{httpContext.Connection.RemoteIpAddress}",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy("ai-generation", httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: $"ai:{httpContext.Connection.RemoteIpAddress}",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        services.AddSingleton<IFacilitatorTokenStore, FacilitatorTokenStore>();
        services.AddSingleton<IParticipantTokenStore, ParticipantTokenStore>();

        services.AddScoped<IApiMapper, ApiMapper>();

        return services;
    }

    public static IServiceCollection AddPulseAIServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddHttpClient();

        services.AddHttpClient("openai", client =>
        {
            var openAiBase = configuration["AI:OpenAI:Endpoint"];
            if (!string.IsNullOrWhiteSpace(openAiBase))
            {
                client.BaseAddress = new Uri(openAiBase);
                Log.Information("OpenAI HttpClient BaseAddress set to: {BaseAddress}", openAiBase);
            }
            else
            {
                Log.Warning("OpenAI endpoint not configured in settings");
            }

            client.Timeout = TimeSpan.FromSeconds(90);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;

            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120);
            options.CircuitBreaker.FailureRatio = 0.7;
            options.CircuitBreaker.MinimumThroughput = 5;

            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
        });

        services.AddSingleton<TechWayFit.Pulse.AI.Http.OpenAIApiClient>();

        var aiEnabled = configuration.GetValue<bool>("AI:Enabled");
        var aiProvider = configuration.GetValue<string>("AI:Provider") ?? "Mock";
        var openAiApiKey = configuration["AI:OpenAI:ApiKey"];

        Log.Information("Environment: {Environment}", environment.EnvironmentName);
        Log.Information("AI Enabled: {Enabled}", aiEnabled);
        Log.Information("AI Provider: {Provider}", aiProvider);
        Log.Information("API Key present: {HasKey}", !string.IsNullOrWhiteSpace(openAiApiKey));

        if (aiEnabled && !string.IsNullOrWhiteSpace(openAiApiKey) && aiProvider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("Registering REAL AI services (OpenAI API enabled)");
            services.AddScoped<IParticipantAIService, TechWayFit.Pulse.AI.Services.ParticipantAIService>();
            services.AddScoped<IFacilitatorAIService, TechWayFit.Pulse.AI.Services.FacilitatorAIService>();
            services.AddScoped<ISessionAIService, TechWayFit.Pulse.AI.Services.SessionAIService>();
            services.AddScoped<IFiveWhysAIService, TechWayFit.Pulse.AI.Services.FiveWhysAIService>();
        }
        else if (aiProvider.Equals("MLNet", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("Registering ML.NET AI services (Microsoft ML.NET machine learning)");
            services.AddScoped<IParticipantAIService, TechWayFit.Pulse.AI.Services.MockParticipantAIService>();
            services.AddScoped<IFacilitatorAIService, TechWayFit.Pulse.AI.Services.MockFacilitatorAIService>();
            services.AddScoped<ISessionAIService, TechWayFit.Pulse.AI.Services.MLNetSessionAIService>();
            services.AddScoped<IFiveWhysAIService, TechWayFit.Pulse.AI.Services.MockFiveWhysAIService>();
        }
        else if (aiProvider.Equals("Intelligent", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("Registering INTELLIGENT AI services (NLP-inspired keyword-based generation)");
            services.AddScoped<IParticipantAIService, TechWayFit.Pulse.AI.Services.MockParticipantAIService>();
            services.AddScoped<IFacilitatorAIService, TechWayFit.Pulse.AI.Services.MockFacilitatorAIService>();
            services.AddScoped<ISessionAIService, TechWayFit.Pulse.AI.Services.IntelligentSessionAIService>();
            services.AddScoped<IFiveWhysAIService, TechWayFit.Pulse.AI.Services.MockFiveWhysAIService>();
        }

        services.AddKeyedScoped<ISessionAIService, TechWayFit.Pulse.AI.Services.IntelligentSessionAIService>("Intelligent");
        services.TryAddScoped<IFiveWhysAIService, TechWayFit.Pulse.AI.Services.MockFiveWhysAIService>();

        services.AddSingleton<IAIWorkQueue, TechWayFit.Pulse.Infrastructure.AI.AIWorkQueue>();
        services.AddHostedService<TechWayFit.Pulse.Web.BackgroundServices.AIProcessingHostedService>();

        return services;
    }

    public static IServiceCollection AddPulseApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPulseDatabase(configuration);

        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IParticipantService, ParticipantService>();
        services.AddScoped<IResponseService, ResponseService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAiQuotaService, AiQuotaService>();
        services.AddScoped<IPollDashboardService, PollDashboardService>();
        services.AddScoped<IWordCloudDashboardService, WordCloudDashboardService>();
        services.AddScoped<IRatingDashboardService, RatingDashboardService>();
        services.AddScoped<IGeneralFeedbackDashboardService, GeneralFeedbackDashboardService>();
        services.AddScoped<IQnADashboardService, QnADashboardService>();
        services.AddScoped<IQuadrantDashboardService, QuadrantDashboardService>();

        services.AddAllActivityPlugins();

        services.AddScoped<ISessionActivityMetadataService, SessionActivityMetadataService>();
        services.AddScoped<ISessionCodeGenerator, SessionCodeGenerator>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ISessionGroupService, SessionGroupService>();
        services.AddScoped<ISessionTemplateService, TechWayFit.Pulse.Infrastructure.Services.SessionTemplateService>();
        services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IDomainEventDispatcher, LoggingDomainEventDispatcher>();

        services.AddSingleton<IFacilitatorTokenService, FacilitatorTokenService>();
        services.AddScoped<IClientTokenService, ClientTokenService>();
        services.AddScoped<IApiKeyProtectionService, ApiKeyProtectionService>();
        services.AddScoped<IHubNotificationService, HubNotificationService>();
        services.AddSingleton<IFileService, FileService>();

        var emailProvider = configuration.GetValue<string>("Email:Provider");
        if (emailProvider?.Equals("Smtp", StringComparison.OrdinalIgnoreCase) == true)
        {
            services.AddScoped<IEmailService, SmtpEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, ConsoleEmailService>();
        }

        return services;
    }

    public static IServiceCollection AddPulseSignalR(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSignalR(options =>
        {
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            options.HandshakeTimeout = TimeSpan.FromSeconds(15);
            options.MaximumReceiveMessageSize = 32 * 1024;
        });

        var useDbBackplane = configuration.GetValue<bool>("SignalR:UseDatabaseBackplane");
        if (useDbBackplane)
        {
            Log.Information("Enabling SignalR database backplane for web farm support");
            services.AddSingleton<TechWayFit.Pulse.Infrastructure.SignalR.DatabaseBackplane.DatabaseBackplaneService>();
            services.AddHostedService<TechWayFit.Pulse.Infrastructure.SignalR.DatabaseBackplane.DatabaseBackplaneService>(
                sp => sp.GetRequiredService<TechWayFit.Pulse.Infrastructure.SignalR.DatabaseBackplane.DatabaseBackplaneService>());
        }
        else
        {
            Log.Warning("SignalR database backplane is disabled. In-memory mode (not suitable for web farms).");
            Log.Information("To enable web farm support, set 'SignalR:UseDatabaseBackplane' to true in appsettings.json");
        }

        return services;
    }

    public static IServiceCollection AddPulseHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Application is healthy"), tags: ["ready"])
            .AddCheck<PulseDatabaseHealthCheck>("database", tags: ["ready"]);

        return services;
    }
}

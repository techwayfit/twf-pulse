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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddSingleton<IFacilitatorTokenStore, FacilitatorTokenStore>();

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapControllers();
app.MapHub<TechWayFit.Pulse.Web.Hubs.WorkshopHub>("/hubs/workshop");
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

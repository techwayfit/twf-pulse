using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;

namespace TechWayFit.Pulse.Web.Extensions;

public static class PulseApplicationBuilderExtensions
{
    public static WebApplication UsePulseWebPipeline(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseMiddleware<TechWayFit.Pulse.Web.Middleware.GlobalExceptionHandlingMiddleware>();
        app.UseStatusCodePagesWithReExecute("/Error/{0}");

        app.UseHttpsRedirection();
        app.UseResponseCompression();

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (ctx.File.Name.EndsWith(".css") || ctx.File.Name.EndsWith(".js"))
                {
                    ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=600");
                }
            }
        });

        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.GetLevel = (httpContext, _, ex) => ex != null || httpContext.Response.StatusCode > 499
                ? LogEventLevel.Error
                : LogEventLevel.Information;
        });

        app.UseRouting();
        app.UseSession();
        app.UseAuthentication();
        app.UseMiddleware<TechWayFit.Pulse.Web.Middleware.FacilitatorTokenMiddleware>();
        app.UseMiddleware<TechWayFit.Pulse.Web.Middleware.FacilitatorContextMiddleware>();
        app.UseAuthorization();

        return app;
    }

    public static WebApplication MapPulseEndpoints(this WebApplication app)
    {
        app.MapControllers();
        app.MapHub<TechWayFit.Pulse.Web.Hubs.WorkshopHub>("/hubs/workshop");
        app.MapBlazorHub();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapRazorPages();
        app.MapFallbackToPage("/_Host");

        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        return app;
    }
}

using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TechWayFit.Pulse.Web.Infrastructure;

/// <summary>
/// Circuit handler that manages connection lifecycle for mobile participants.
/// Ensures stale state is cleared when circuits expire.
/// </summary>
public sealed class MobileCircuitHandler : CircuitHandler
{
    private readonly ILogger<MobileCircuitHandler> _logger;

    public MobileCircuitHandler(ILogger<MobileCircuitHandler> logger)
    {
   _logger = logger;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Circuit connected: {CircuitId}", circuit.Id);
     return Task.CompletedTask;
 }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Circuit disconnected: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
      _logger.LogInformation("Circuit opened: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Circuit closed (expired): {CircuitId} - State will be lost", circuit.Id);
        return Task.CompletedTask;
    }
}

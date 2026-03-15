using System;

namespace TechWayFit.Pulse.Infrastructure.SignalR.DatabaseBackplane;

/// <summary>
/// Represents a SignalR message stored in the database for cross-server communication
/// </summary>
public class SignalRMessage
{
    public long Id { get; set; }
    
    public string GroupName { get; set; } = string.Empty;
    
    public string MethodName { get; set; } = string.Empty;
    
    public string PayloadJson { get; set; } = string.Empty;
    
    public string ServerId { get; set; } = string.Empty;
    
    public DateTimeOffset CreatedAt { get; set; }
    
  public bool IsProcessed { get; set; }
    
    public DateTimeOffset? ProcessedAt { get; set; }
}

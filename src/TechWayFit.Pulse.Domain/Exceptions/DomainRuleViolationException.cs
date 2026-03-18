namespace TechWayFit.Pulse.Domain.Exceptions;

public sealed class DomainRuleViolationException : InvalidOperationException
{
    public DomainRuleViolationException(string message)
        : base(message)
    {
    }
}

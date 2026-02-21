using TechWayFit.Pulse.BackOffice.Core.Entities;
using TechWayFit.Pulse.BackOffice.Core.Models.Auth;

namespace TechWayFit.Pulse.BackOffice.Core.Abstractions;

/// <summary>
/// Authentication and account management for BackOffice operators.
/// </summary>
public interface IBackOfficeAuthService
{
    /// <summary>Validate credentials and return the operator if valid; null if invalid.</summary>
    Task<BackOfficeUser?> ValidateCredentialsAsync(string username, string password, CancellationToken ct = default);

    Task<IReadOnlyList<BackOfficeUserSummary>> ListOperatorsAsync(CancellationToken ct = default);

    /// <summary>Create a new operator. Password is hashed internally.</summary>
    Task<BackOfficeUser> CreateOperatorAsync(CreateBackOfficeUserRequest request, string createdByOperatorId, CancellationToken ct = default);

    Task UpdateRoleAsync(UpdateBackOfficeUserRoleRequest request, string operatorId, string ipAddress, CancellationToken ct = default);

    Task ToggleActiveAsync(ToggleBackOfficeUserActiveRequest request, string operatorId, string ipAddress, CancellationToken ct = default);
}

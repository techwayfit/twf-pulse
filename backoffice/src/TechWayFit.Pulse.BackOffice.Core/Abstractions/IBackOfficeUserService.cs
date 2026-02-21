using TechWayFit.Pulse.BackOffice.Core.Models.Users;

namespace TechWayFit.Pulse.BackOffice.Core.Abstractions;

/// <summary>
/// BackOffice operations on FacilitatorUser accounts.
/// All mutating methods write an audit record before committing.
/// </summary>
public interface IBackOfficeUserService
{
    Task<UserSearchResult> SearchAsync(UserSearchQuery query, CancellationToken ct = default);
    Task<UserDetailViewModel?> GetDetailAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Disable or re-enable a facilitator account.</summary>
    Task SetDisabledAsync(DisableUserRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default);

    /// <summary>Update a facilitator's display name (Operator or SuperAdmin).</summary>
    Task UpdateDisplayNameAsync(UpdateUserDisplayNameRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default);

    /// <summary>Update a facilitator's email address (SuperAdmin only).</summary>
    Task UpdateEmailAsync(UpdateUserEmailRequest request, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default);

    /// <summary>Hard-delete a facilitator user and all associated data (SuperAdmin only).</summary>
    Task DeleteUserAsync(Guid userId, string confirmationEmail, string reason, string operatorId, string operatorRole, string ipAddress, CancellationToken ct = default);
}

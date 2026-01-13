using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Abstractions.Repositories;

public interface ILoginOtpRepository
{
 Task<LoginOtp?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LoginOtp?> GetValidOtpAsync(string email, string otpCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoginOtp>> GetRecentOtpsForEmailAsync(string email, int count, CancellationToken cancellationToken = default);

    Task AddAsync(LoginOtp otp, CancellationToken cancellationToken = default);

    Task UpdateAsync(LoginOtp otp, CancellationToken cancellationToken = default);

    Task DeleteExpiredAsync(DateTimeOffset before, CancellationToken cancellationToken = default);
}

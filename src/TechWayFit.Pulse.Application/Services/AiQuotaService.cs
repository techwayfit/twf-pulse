using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Abstractions.Services;
using TechWayFit.Pulse.Domain.Entities;

namespace TechWayFit.Pulse.Application.Services;

public sealed class AiQuotaService : IAiQuotaService
{
    private readonly IFacilitatorUserDataRepository _userDataRepository;
    private readonly AiQuotaOptions _options;
    private readonly ILogger<AiQuotaService> _logger;

    public AiQuotaService(
        IFacilitatorUserDataRepository userDataRepository,
        IOptions<AiQuotaOptions> options,
        ILogger<AiQuotaService> logger)
    {
        _userDataRepository = userDataRepository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<QuotaCheckResult> CheckQuotaAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default)
    {
        // If quota system is disabled, allow unlimited
        if (!_options.Enabled)
        {
            return new QuotaCheckResult(true, "Unlimited", 0, int.MaxValue, null, null);
        }

        // Check if user has their own API key (BYOK = unlimited)
        var apiKeyData = await _userDataRepository.GetByKeyAsync(
            facilitatorUserId, 
            FacilitatorUserDataKeys.OpenAiApiKey, 
            cancellationToken);

        if (apiKeyData != null && !string.IsNullOrWhiteSpace(apiKeyData.Value))
        {
            return new QuotaCheckResult(true, "BYOK", 0, int.MaxValue, null, "Using your own API key");
        }

        // Reset quota if needed
        await ResetQuotaIfNeededAsync(facilitatorUserId, cancellationToken);

        // Get current usage
        var usedSessionsData = await _userDataRepository.GetByKeyAsync(
            facilitatorUserId,
            FacilitatorUserDataKeys.AiQuotaUsedSessions,
            cancellationToken);

        var usedSessions = 0;
        if (usedSessionsData != null && int.TryParse(usedSessionsData.Value, out var parsed))
        {
            usedSessions = parsed;
        }

        var resetDateData = await _userDataRepository.GetByKeyAsync(
            facilitatorUserId,
            FacilitatorUserDataKeys.AiQuotaResetDate,
            cancellationToken);

        DateTimeOffset? resetDate = null;
        if (resetDateData != null && DateTimeOffset.TryParse(resetDateData.Value, out var parsedDate))
        {
            resetDate = parsedDate;
        }

        var hasQuota = usedSessions < _options.FreeSessionsPerMonth;
        var message = hasQuota 
            ? null 
            : $"You've used all {_options.FreeSessionsPerMonth} free AI sessions this month. Add your own API key for unlimited access.";

        return new QuotaCheckResult(
            hasQuota,
            "Free",
            usedSessions,
            _options.FreeSessionsPerMonth,
            resetDate,
            message);
    }

    public async Task ConsumeQuotaAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return; // Quota disabled
        }

        // Check if BYOK (don't consume quota)
        var apiKeyData = await _userDataRepository.GetByKeyAsync(
            facilitatorUserId,
            FacilitatorUserDataKeys.OpenAiApiKey,
            cancellationToken);

        if (apiKeyData != null && !string.IsNullOrWhiteSpace(apiKeyData.Value))
        {
            _logger.LogInformation("User {UserId} using BYOK - not consuming quota", facilitatorUserId);
            return; // Using own key, don't consume quota
        }

        // Reset if needed
        await ResetQuotaIfNeededAsync(facilitatorUserId, cancellationToken);

        // Get current usage
        var usedSessionsData = await _userDataRepository.GetByKeyAsync(
            facilitatorUserId,
            FacilitatorUserDataKeys.AiQuotaUsedSessions,
            cancellationToken);

        var usedSessions = 0;
        if (usedSessionsData != null && int.TryParse(usedSessionsData.Value, out var parsed))
        {
            usedSessions = parsed;
        }

        // Increment usage
        usedSessions++;
        await _userDataRepository.SetValueAsync(
            facilitatorUserId,
            FacilitatorUserDataKeys.AiQuotaUsedSessions,
            usedSessions.ToString(),
            cancellationToken);

        _logger.LogInformation(
            "AI quota consumed for user {UserId}: {Used}/{Total}",
            facilitatorUserId,
            usedSessions,
            _options.FreeSessionsPerMonth);

        // Set tier
        await _userDataRepository.SetValueAsync(
            facilitatorUserId,
            FacilitatorUserDataKeys.AiQuotaTier,
            "Free",
            cancellationToken);
    }

    public async Task<QuotaCheckResult> GetQuotaStatusAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default)
    {
        return await CheckQuotaAsync(facilitatorUserId, cancellationToken);
    }

    public async Task ResetQuotaIfNeededAsync(Guid facilitatorUserId, CancellationToken cancellationToken = default)
    {
        var resetDateData = await _userDataRepository.GetByKeyAsync(
            facilitatorUserId,
            FacilitatorUserDataKeys.AiQuotaResetDate,
            cancellationToken);

        DateTimeOffset resetDate;
        var now = DateTimeOffset.UtcNow;

        if (resetDateData == null || !DateTimeOffset.TryParse(resetDateData.Value, out resetDate))
        {
            // First time - set reset date to next month
            resetDate = GetNextResetDate(now);
            await _userDataRepository.SetValueAsync(
                facilitatorUserId,
                FacilitatorUserDataKeys.AiQuotaResetDate,
                resetDate.ToString("O"),
                cancellationToken);
            
            await _userDataRepository.SetValueAsync(
                facilitatorUserId,
                FacilitatorUserDataKeys.AiQuotaUsedSessions,
                "0",
                cancellationToken);
            
            _logger.LogInformation("Initialized quota for user {UserId}, reset date: {ResetDate}", facilitatorUserId, resetDate);
            return;
        }

        // Check if reset date has passed
        if (now >= resetDate)
        {
            // Reset quota
            await _userDataRepository.SetValueAsync(
                facilitatorUserId,
                FacilitatorUserDataKeys.AiQuotaUsedSessions,
                "0",
                cancellationToken);

            // Set new reset date
            var newResetDate = GetNextResetDate(now);
            await _userDataRepository.SetValueAsync(
                facilitatorUserId,
                FacilitatorUserDataKeys.AiQuotaResetDate,
                newResetDate.ToString("O"),
                cancellationToken);

            _logger.LogInformation(
                "Reset quota for user {UserId}, new reset date: {ResetDate}",
                facilitatorUserId,
                newResetDate);
        }
    }

    private static DateTimeOffset GetNextResetDate(DateTimeOffset fromDate)
    {
        // Reset on the 1st of next month at midnight UTC
        var nextMonth = fromDate.AddMonths(1);
        return new DateTimeOffset(nextMonth.Year, nextMonth.Month, 1, 0, 0, 0, TimeSpan.Zero);
    }
}

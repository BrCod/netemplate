using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Netemplate.Application.Services;
using Netemplate.Infrastructure.FeatureFlags.DTOs;
using Netemplate.Infrastructure.FeatureFlags.Entities;

namespace Netemplate.Infrastructure.FeatureFlags.Services;

/// <summary>
/// Feature flag service with audit trail persistence and caching
/// </summary>
public sealed class PersistentFeatureFlagService : IFeatureFlagService
{
    private readonly FeatureFlagsDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PersistentFeatureFlagService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public PersistentFeatureFlagService(
        FeatureFlagsDbContext dbContext,
        IMemoryCache cache,
        ILogger<PersistentFeatureFlagService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(featureName))
        {
            _logger.LogWarning("Feature flag check called with null or empty feature name");
            return false;
        }

        var cacheKey = $"FeatureFlag:{featureName}";

        // Try cache first
        if (_cache.TryGetValue<bool>(cacheKey, out var cachedValue))
        {
            _logger.LogDebug("Feature flag {FeatureName} resolved from cache: {IsEnabled}", featureName, cachedValue);
            return cachedValue;
        }

        // Query database
        var flag = await _dbContext.FeatureFlags
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Name == featureName, ct);

        var isEnabled = flag?.IsEnabled ?? false;

        // Cache the result
        _cache.Set(cacheKey, isEnabled, CacheDuration);

        _logger.LogDebug("Feature flag {FeatureName} resolved from database: {IsEnabled}", featureName, isEnabled);
        return isEnabled;
    }

    /// <summary>
    /// Updates a feature flag state with audit trail
    /// </summary>
    public async Task<bool> SetFlagAsync(FeatureFlagChangeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.FeatureName))
        {
            _logger.LogWarning("Feature flag update called with null or empty feature name");
            return false;
        }

        using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            var flag = await _dbContext.FeatureFlags
                .FirstOrDefaultAsync(f => f.Name == request.FeatureName, ct);

            var now = DateTime.UtcNow;

            if (flag == null)
            {
                // Create new flag
                flag = new FeatureFlag
                {
                    Id = Guid.NewGuid(),
                    Name = request.FeatureName,
                    IsEnabled = request.IsEnabled,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = request.ChangedBy,
                    UpdatedBy = request.ChangedBy
                };

                _dbContext.FeatureFlags.Add(flag);

                // Log creation as audit entry (previous value = false for new flags)
                var auditLog = new FeatureFlagAuditLog
                {
                    Id = Guid.NewGuid(),
                    FeatureFlagId = flag.Id,
                    FeatureFlagName = flag.Name,
                    PreviousValue = false,
                    NewValue = request.IsEnabled,
                    ChangedAt = now,
                    ChangedBy = request.ChangedBy,
                    Reason = request.Reason ?? "Initial creation",
                    CorrelationId = request.CorrelationId,
                    IpAddress = request.IpAddress,
                    UserAgent = request.UserAgent
                };

                _dbContext.FeatureFlagAuditLogs.Add(auditLog);

                _logger.LogInformation(
                    "Created feature flag {FeatureName} with value {IsEnabled} by {ChangedBy}",
                    request.FeatureName, request.IsEnabled, request.ChangedBy ?? "system");
            }
            else
            {
                // Update existing flag
                var previousValue = flag.IsEnabled;

                if (previousValue == request.IsEnabled)
                {
                    _logger.LogDebug("Feature flag {FeatureName} already has value {IsEnabled}, no change needed",
                        request.FeatureName, request.IsEnabled);
                    return true;
                }

                flag.IsEnabled = request.IsEnabled;
                flag.UpdatedAt = now;
                flag.UpdatedBy = request.ChangedBy;

                // Log change to audit trail
                var auditLog = new FeatureFlagAuditLog
                {
                    Id = Guid.NewGuid(),
                    FeatureFlagId = flag.Id,
                    FeatureFlagName = flag.Name,
                    PreviousValue = previousValue,
                    NewValue = request.IsEnabled,
                    ChangedAt = now,
                    ChangedBy = request.ChangedBy,
                    Reason = request.Reason,
                    CorrelationId = request.CorrelationId,
                    IpAddress = request.IpAddress,
                    UserAgent = request.UserAgent
                };

                _dbContext.FeatureFlagAuditLogs.Add(auditLog);

                _logger.LogInformation(
                    "Updated feature flag {FeatureName} from {PreviousValue} to {NewValue} by {ChangedBy}. Reason: {Reason}",
                    request.FeatureName, previousValue, request.IsEnabled, request.ChangedBy ?? "system", request.Reason ?? "none");
            }

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            // Invalidate cache
            var cacheKey = $"FeatureFlag:{request.FeatureName}";
            _cache.Remove(cacheKey);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update feature flag {FeatureName}", request.FeatureName);
            await transaction.RollbackAsync(ct);
            return false;
        }
    }

    /// <summary>
    /// Retrieves audit history for a specific feature flag
    /// </summary>
    public async Task<IReadOnlyList<FeatureFlagAuditEntry>> GetAuditHistoryAsync(
        string featureName,
        int limit = 100,
        CancellationToken ct = default)
    {
        var auditLogs = await _dbContext.FeatureFlagAuditLogs
            .AsNoTracking()
            .Where(a => a.FeatureFlagName == featureName)
            .OrderByDescending(a => a.ChangedAt)
            .Take(limit)
            .ToListAsync(ct);

        return auditLogs.Select(a => new FeatureFlagAuditEntry
        {
            Id = a.Id,
            FeatureFlagId = a.FeatureFlagId,
            FeatureFlagName = a.FeatureFlagName,
            PreviousValue = a.PreviousValue,
            NewValue = a.NewValue,
            ChangedAt = a.ChangedAt,
            ChangedBy = a.ChangedBy,
            Reason = a.Reason,
            CorrelationId = a.CorrelationId,
            IpAddress = a.IpAddress,
            UserAgent = a.UserAgent
        }).ToList();
    }

    /// <summary>
    /// Retrieves all audit logs within a time range
    /// </summary>
    public async Task<IReadOnlyList<FeatureFlagAuditEntry>> GetAuditHistoryAsync(
        DateTime startDate,
        DateTime endDate,
        int limit = 1000,
        CancellationToken ct = default)
    {
        var auditLogs = await _dbContext.FeatureFlagAuditLogs
            .AsNoTracking()
            .Where(a => a.ChangedAt >= startDate && a.ChangedAt <= endDate)
            .OrderByDescending(a => a.ChangedAt)
            .Take(limit)
            .ToListAsync(ct);

        return auditLogs.Select(a => new FeatureFlagAuditEntry
        {
            Id = a.Id,
            FeatureFlagId = a.FeatureFlagId,
            FeatureFlagName = a.FeatureFlagName,
            PreviousValue = a.PreviousValue,
            NewValue = a.NewValue,
            ChangedAt = a.ChangedAt,
            ChangedBy = a.ChangedBy,
            Reason = a.Reason,
            CorrelationId = a.CorrelationId,
            IpAddress = a.IpAddress,
            UserAgent = a.UserAgent
        }).ToList();
    }

    /// <summary>
    /// Retrieves all feature flags with their current state
    /// </summary>
    public async Task<IReadOnlyList<FeatureFlag>> GetAllFlagsAsync(CancellationToken ct = default)
    {
        return await _dbContext.FeatureFlags
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .ToListAsync(ct);
    }
}

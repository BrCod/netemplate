using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Netemplate.Infrastructure.FeatureFlags;
using Netemplate.Infrastructure.FeatureFlags.DTOs;
using Netemplate.Infrastructure.FeatureFlags.Services;

namespace Netemplate.Infrastructure.FeatureFlags.Tests;

public sealed class PersistentFeatureFlagServiceTests : IDisposable
{
    private readonly FeatureFlagsDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly PersistentFeatureFlagService _service;

    public PersistentFeatureFlagServiceTests()
    {
        var options = new DbContextOptionsBuilder<FeatureFlagsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
            {
                // Suppress transaction warnings for in-memory database
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning);
            })
            .Options;

        _dbContext = new FeatureFlagsDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new PersistentFeatureFlagService(_dbContext, _cache, NullLogger<PersistentFeatureFlagService>.Instance);
    }

    [Fact]
    public async Task IsEnabledAsync_WithNonExistentFlag_ReturnsFalse()
    {
        // Act
        var result = await _service.IsEnabledAsync("NonExistentFlag");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEnabledAsync_WithNullOrEmptyName_ReturnsFalse()
    {
        // Act
        var result1 = await _service.IsEnabledAsync(null!);
        var result2 = await _service.IsEnabledAsync("");
        var result3 = await _service.IsEnabledAsync("   ");

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
        result3.Should().BeFalse();
    }

    [Fact]
    public async Task SetFlagAsync_WithNewFlag_CreatesFeatureFlagAndAuditLog()
    {
        // Arrange
        var request = new FeatureFlagChangeRequest
        {
            FeatureName = "NewFeature",
            IsEnabled = true,
            ChangedBy = "test-user",
            Reason = "Initial setup",
            CorrelationId = "test-correlation-id"
        };

        // Act
        var result = await _service.SetFlagAsync(request);

        // Assert
        result.Should().BeTrue();

        var flag = await _dbContext.FeatureFlags.FirstOrDefaultAsync(f => f.Name == "NewFeature");
        flag.Should().NotBeNull();
        flag!.IsEnabled.Should().BeTrue();
        flag.CreatedBy.Should().Be("test-user");
        flag.UpdatedBy.Should().Be("test-user");

        var auditLog = await _dbContext.FeatureFlagAuditLogs.FirstOrDefaultAsync(a => a.FeatureFlagName == "NewFeature");
        auditLog.Should().NotBeNull();
        auditLog!.PreviousValue.Should().BeFalse(); // New flags start as disabled
        auditLog.NewValue.Should().BeTrue();
        auditLog.ChangedBy.Should().Be("test-user");
        auditLog.Reason.Should().Be("Initial setup");
        auditLog.CorrelationId.Should().Be("test-correlation-id");
    }

    [Fact]
    public async Task SetFlagAsync_WithExistingFlag_UpdatesValueAndCreatesAuditLog()
    {
        // Arrange - Create initial flag
        var initialRequest = new FeatureFlagChangeRequest
        {
            FeatureName = "ExistingFeature",
            IsEnabled = false,
            ChangedBy = "user1",
            Reason = "Initial creation"
        };
        await _service.SetFlagAsync(initialRequest);

        // Act - Update flag
        var updateRequest = new FeatureFlagChangeRequest
        {
            FeatureName = "ExistingFeature",
            IsEnabled = true,
            ChangedBy = "user2",
            Reason = "Enabling feature for testing",
            CorrelationId = "update-correlation",
            IpAddress = "192.168.1.1",
            UserAgent = "TestAgent/1.0"
        };
        var result = await _service.SetFlagAsync(updateRequest);

        // Assert
        result.Should().BeTrue();

        var flag = await _dbContext.FeatureFlags.FirstOrDefaultAsync(f => f.Name == "ExistingFeature");
        flag.Should().NotBeNull();
        flag!.IsEnabled.Should().BeTrue();
        flag.UpdatedBy.Should().Be("user2");

        var auditLogs = await _dbContext.FeatureFlagAuditLogs
            .Where(a => a.FeatureFlagName == "ExistingFeature")
            .OrderBy(a => a.ChangedAt)
            .ToListAsync();

        auditLogs.Should().HaveCount(2);

        var updateLog = auditLogs[1];
        updateLog.PreviousValue.Should().BeFalse();
        updateLog.NewValue.Should().BeTrue();
        updateLog.ChangedBy.Should().Be("user2");
        updateLog.Reason.Should().Be("Enabling feature for testing");
        updateLog.CorrelationId.Should().Be("update-correlation");
        updateLog.IpAddress.Should().Be("192.168.1.1");
        updateLog.UserAgent.Should().Be("TestAgent/1.0");
    }

    [Fact]
    public async Task SetFlagAsync_WithSameValue_SkipsUpdateButReturnsTrue()
    {
        // Arrange
        var request = new FeatureFlagChangeRequest
        {
            FeatureName = "UnchangedFeature",
            IsEnabled = true,
            ChangedBy = "user1"
        };
        await _service.SetFlagAsync(request);

        var initialAuditCount = await _dbContext.FeatureFlagAuditLogs.CountAsync();

        // Act - Try to set same value
        var result = await _service.SetFlagAsync(request);

        // Assert
        result.Should().BeTrue();

        var finalAuditCount = await _dbContext.FeatureFlagAuditLogs.CountAsync();
        finalAuditCount.Should().Be(initialAuditCount); // No new audit log created
    }

    [Fact]
    public async Task IsEnabledAsync_WithCachedValue_ReturnsFromCache()
    {
        // Arrange
        var request = new FeatureFlagChangeRequest
        {
            FeatureName = "CachedFeature",
            IsEnabled = true,
            ChangedBy = "test-user"
        };
        await _service.SetFlagAsync(request);

        // First call to populate cache
        await _service.IsEnabledAsync("CachedFeature");

        // Manually change database value
        var flag = await _dbContext.FeatureFlags.FirstAsync(f => f.Name == "CachedFeature");
        flag.IsEnabled = false;
        await _dbContext.SaveChangesAsync();

        // Act - Should return cached value (true) not database value (false)
        var result = await _service.IsEnabledAsync("CachedFeature");

        // Assert
        result.Should().BeTrue(); // Cache should still have old value
    }

    [Fact]
    public async Task SetFlagAsync_InvalidatesCacheAfterUpdate()
    {
        // Arrange
        var request = new FeatureFlagChangeRequest
        {
            FeatureName = "CacheInvalidationTest",
            IsEnabled = true,
            ChangedBy = "test-user"
        };
        await _service.SetFlagAsync(request);

        // Populate cache
        await _service.IsEnabledAsync("CacheInvalidationTest");

        // Act - Update flag (should invalidate cache)
        var updateRequest = new FeatureFlagChangeRequest
        {
            FeatureName = "CacheInvalidationTest",
            IsEnabled = false,
            ChangedBy = "test-user",
            Reason = "Testing cache invalidation"
        };
        await _service.SetFlagAsync(updateRequest);

        // Read again - should get new value from database
        var result = await _service.IsEnabledAsync("CacheInvalidationTest");

        // Assert
        result.Should().BeFalse(); // Should reflect updated value
    }

    [Fact]
    public async Task GetAuditHistoryAsync_ReturnsHistoryForSpecificFlag()
    {
        // Arrange
        var flagName = "AuditTestFeature";

        // Create multiple changes
        await _service.SetFlagAsync(new FeatureFlagChangeRequest
        {
            FeatureName = flagName,
            IsEnabled = true,
            ChangedBy = "user1",
            Reason = "First change"
        });

        await _service.SetFlagAsync(new FeatureFlagChangeRequest
        {
            FeatureName = flagName,
            IsEnabled = false,
            ChangedBy = "user2",
            Reason = "Second change"
        });

        await _service.SetFlagAsync(new FeatureFlagChangeRequest
        {
            FeatureName = flagName,
            IsEnabled = true,
            ChangedBy = "user3",
            Reason = "Third change"
        });

        // Act
        var history = await _service.GetAuditHistoryAsync(flagName);

        // Assert
        history.Should().HaveCount(3);
        history.Should().BeInDescendingOrder(h => h.ChangedAt); // Most recent first
        history[0].ChangedBy.Should().Be("user3");
        history[1].ChangedBy.Should().Be("user2");
        history[2].ChangedBy.Should().Be("user1");
    }

    [Fact]
    public async Task GetAuditHistoryAsync_WithLimit_ReturnsLimitedResults()
    {
        // Arrange
        var flagName = "LimitTestFeature";

        for (int i = 0; i < 10; i++)
        {
            await _service.SetFlagAsync(new FeatureFlagChangeRequest
            {
                FeatureName = flagName,
                IsEnabled = i % 2 == 0,
                ChangedBy = $"user{i}",
                Reason = $"Change {i}"
            });
        }

        // Act
        var history = await _service.GetAuditHistoryAsync(flagName, limit: 5);

        // Assert
        history.Should().HaveCount(5);
        history.Should().BeInDescendingOrder(h => h.ChangedAt);
    }

    [Fact]
    public async Task GetAuditHistoryAsync_WithDateRange_ReturnsFilteredResults()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddHours(-2);
        var endDate = DateTime.UtcNow.AddHours(2);

        await _service.SetFlagAsync(new FeatureFlagChangeRequest
        {
            FeatureName = "DateRangeTest1",
            IsEnabled = true,
            ChangedBy = "user1"
        });

        await _service.SetFlagAsync(new FeatureFlagChangeRequest
        {
            FeatureName = "DateRangeTest2",
            IsEnabled = true,
            ChangedBy = "user2"
        });

        // Act
        var history = await _service.GetAuditHistoryAsync(startDate, endDate);

        // Assert
        history.Should().HaveCount(2);
        history.All(h => h.ChangedAt >= startDate && h.ChangedAt <= endDate).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllFlagsAsync_ReturnsAllFlags()
    {
        // Arrange
        await _service.SetFlagAsync(new FeatureFlagChangeRequest
        {
            FeatureName = "Flag1",
            IsEnabled = true,
            ChangedBy = "user1"
        });

        await _service.SetFlagAsync(new FeatureFlagChangeRequest
        {
            FeatureName = "Flag2",
            IsEnabled = false,
            ChangedBy = "user2"
        });

        await _service.SetFlagAsync(new FeatureFlagChangeRequest
        {
            FeatureName = "Flag3",
            IsEnabled = true,
            ChangedBy = "user3"
        });

        // Act
        var flags = await _service.GetAllFlagsAsync();

        // Assert
        flags.Should().HaveCount(3);
        flags.Should().BeInAscendingOrder(f => f.Name);
        flags.Select(f => f.Name).Should().Contain(new[] { "Flag1", "Flag2", "Flag3" });
    }

    [Fact]
    public async Task SetFlagAsync_WithNullOrEmptyName_ReturnsFalse()
    {
        // Act
        var result1 = await _service.SetFlagAsync(new FeatureFlagChangeRequest
        {
            FeatureName = null!,
            IsEnabled = true
        });

        var result2 = await _service.SetFlagAsync(new FeatureFlagChangeRequest
        {
            FeatureName = "",
            IsEnabled = true
        });

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();

        var flags = await _dbContext.FeatureFlags.ToListAsync();
        flags.Should().BeEmpty();
    }

    [Fact]
    public async Task AuditLog_CapturesAllMetadata()
    {
        // Arrange
        var request = new FeatureFlagChangeRequest
        {
            FeatureName = "MetadataTest",
            IsEnabled = true,
            ChangedBy = "admin@example.com",
            Reason = "Testing metadata capture",
            CorrelationId = "abc-123-def-456",
            IpAddress = "10.0.0.1",
            UserAgent = "Mozilla/5.0 TestBrowser"
        };

        // Act
        await _service.SetFlagAsync(request);

        // Assert
        var auditLog = await _dbContext.FeatureFlagAuditLogs.FirstAsync();
        auditLog.ChangedBy.Should().Be("admin@example.com");
        auditLog.Reason.Should().Be("Testing metadata capture");
        auditLog.CorrelationId.Should().Be("abc-123-def-456");
        auditLog.IpAddress.Should().Be("10.0.0.1");
        auditLog.UserAgent.Should().Be("Mozilla/5.0 TestBrowser");
        auditLog.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _cache.Dispose();
    }
}

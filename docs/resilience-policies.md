# Resilience Policies for Clean Architecture API Template

**Purpose**: Document default and recommended resilience strategies for all external adapter calls (DB, cache, messaging).

## Principles
- All external calls MUST use standardized resilience policies: retry, circuit breaker, timeout, bulkhead (where applicable).
- Policies implemented via Polly (or equivalent) and registered centrally in DI.
- No ad-hoc sleeps, infinite retries, or unbounded error swallowing.

## Retry Policy
- Exponential backoff with jitter
- Max attempts: 5
- Initial delay: 200ms
- Max delay: 2s
- Applies to transient errors (network, timeout, deadlock)

## Circuit Breaker
- Opens after 5 consecutive failures
- Duration: 30s
- Half-open trial: 2 requests
- Applies to persistent errors (connection refused, auth failure)

## Timeout
- Per-call timeout: 3s (configurable per adapter)
- Fails fast on hung calls

## Bulkhead
- Max parallel calls: 20 (per adapter)
- Queue limit: 50
- Prevents resource exhaustion

## Configuration
- All thresholds and durations are configurable via environment or appsettings.json
- Policies can be toggled or tuned without code change

## Integration Points
- DB: All repository calls
- Cache: All get/set/delete operations
- Messaging: Publish/subscribe, outbox dispatcher

## Monitoring
- Metrics exported: retry count, circuit breaker open/close events, timeout rate, bulkhead queue length
- Alerts: Circuit breaker open > 5min, retry rate > 10% of calls

## Testing
- Fault injection tests MUST verify retry and circuit breaker behavior
- CI pipeline includes simulated transient and persistent error scenarios

## References
- Polly documentation: https://github.com/App-vNext/Polly
- Microsoft Docs: https://learn.microsoft.com/en-us/dotnet/architecture/cloud-native/resiliency


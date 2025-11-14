# Research: Clean Architecture API Template

**Date**: 2025-11-13
**Branch**: 001-clean-arch-api
**Spec**: spec.md

## Decisions & Rationale

### Storage Abstraction (EF Core default + optional Dapper)
- **Decision**: Use EF Core (Npgsql) as primary persistence; provide repository interfaces enabling alternative Dapper implementation.
- **Rationale**: EF Core gives rapid development (migrations, change tracking) while abstraction permits opt-in performance tuning or micro-optimizations via Dapper.
- **Alternatives Considered**: Raw ADO.NET (too verbose), Dapper-only (loses migrations & modeling convenience), Document DB (misaligned with relational needs).

### Caching Strategy (Redis Cache-Aside)
- **Decision**: Implement cache-aside pattern with tenant-aware keys and TTL config.
- **Rationale**: Simple mental model, avoids stale writes complexity; replaceable adapter.
- **Alternatives**: Write-through (adds latency); write-behind (complex consistency); in-memory only (not cross-instance scalable).

### Messaging (RabbitMQ + Envelope)
- **Decision**: RabbitMQ client library with unified envelope containing correlationId, causationId, tenantId, schemaVersion, timestamp.
- **Rationale**: Mature broker, routing flexibility; envelope ensures traceability & evolution.
- **Alternatives**: Kafka (higher ops overhead for template), Azure Service Bus (paid/cloud-specific), direct HTTP callbacks (tight coupling, reliability issues).

### Validation Pipeline
- **Decision**: Combine DataAnnotations with custom validators registered in DI to run before use case/endpoint logic.
- **Rationale**: Lightweight, no MediatR dependency, composable extension point.
- **Alternatives**: FluentValidation (extra dependency), MediatR pipeline behaviors (rejected due to no paid/heavy packages principle), manual validation in controllers (scattered logic).

### Authentication & Authorization
- **Decision**: Microsoft.AspNetCore.Authentication.JwtBearer with role and policy-based authorization; deny-by-default.
- **Rationale**: Native integration, minimal overhead, flexible claims/scopes.
- **Alternatives**: OAuth2 external providers (adds setup complexity not required for template), cookie auth (less suitable for APIs).

### Observability & Metrics
- **Decision**: Structured logging (ILogger) + optional OpenTelemetry instrumentation; health checks for DB, cache, messaging; metrics via EventCounters or OTel.
- **Rationale**: Leverages built-in extensibility; optional instrumentation keeps base lean.
- **Alternatives**: Custom logging framework (unnecessary), full tracing mandatory (increases complexity for minimal template).

### Rate Limiting
- **Decision**: Use built-in Microsoft.AspNetCore.RateLimiting with sliding window/burst configs.
- **Rationale**: Native support reduces maintenance; aligns with simplicity principle.
- **Alternatives**: Custom middleware (reinvents wheel), external API gateway enforcement only (template loses local guarantee).

### API Versioning
- **Decision**: Use Microsoft.AspNetCore.Mvc.Versioning with URL + header based strategies for flexibility.
- **Rationale**: Clear path for breaking changes tracking; industry standard pattern.
- **Alternatives**: Single version (limits evolution), query parameter versioning (less explicit).

### Error Handling & Problem+JSON
- **Decision**: Central middleware mapping domain/application/infrastructure exceptions to RFC 7807 problem documents with correlationId.
- **Rationale**: Uniform client experience and debuggability.
- **Alternatives**: Ad-hoc try/catch per endpoint (inconsistent), exposing raw stack traces (security risk).

### Replaceability Documentation
- **Decision**: Provide guides showing adapter interface implementation steps and DI registration changes.
- **Rationale**: Encourages extension without modifying core layers.
- **Alternatives**: Implicit knowledge (slows adoption), heavy plugin framework (unnecessary weight).

### Test Strategy
- **Decision**: Domain/Application unit tests (target ≥95% handler coverage) + Integration tests with Testcontainers/docker-compose for Postgres, Redis, RabbitMQ + smoke tests for health endpoints.
- **Rationale**: Balances confidence and speed while reflecting architecture boundaries.
- **Alternatives**: Only unit tests (miss integration issues), only integration tests (slower feedback, harder isolation).

## Unresolved Items
None (all clarifications resolved during spec and planning).

## Risks & Mitigations
- **Performance under heavy messaging load**: Provide guidance for switching to batch publish techniques; metrics to detect backlog.
- **EF Core abstraction leakage**: Keep repositories minimal; avoid leaking DbContext to application layer.
- **Redis inconsistency after bulk updates**: Provide cache invalidation helper with context-based key pattern.
- **Developer misuse of envelope metadata**: Enforce factory method generating standardized envelope.

## Next Steps
Proceed to Phase 1 design artifacts: data-model, contracts, quickstart, then finalize tasks in Phase 2.

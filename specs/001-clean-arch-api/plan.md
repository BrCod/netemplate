# Implementation Plan: Clean Architecture API Template

**Branch**: `001-clean-arch-api` | **Date**: 2025-11-13 | **Spec**: `specs/001-clean-arch-api/spec.md`
**Input**: Production-grade .NET 8 Clean Architecture API template (secure, event-driven, DI-first, JWT, Redis, PostgreSQL, RabbitMQ, envelopes, validation, error mapping, rate limiting, Swagger, OWASP mitigations, health monitoring, no paid packages).

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Deliver a reusable .NET 8 API template enforcing Clean Architecture, DI-first replaceable adapters, event-driven messaging with metadata envelopes, secure JWT auth, centralized validation, structured logging, problem+json error mapping, performance baselines, observability (health + metrics), and documented scaffolding. Technical approach: domain purity, application-level orchestration via interfaces, infrastructure adapters behind contracts, minimal dependencies, containerized local environment, OpenAPI + versioning, and CI pipeline for deterministic validation.

## Technical Context

**Language/Version**: .NET 8 (C# 12)  
**Primary Dependencies**: ASP.NET Core, Swashbuckle.AspNetCore, Microsoft.AspNetCore.Authentication.JwtBearer, Microsoft.AspNetCore.RateLimiting, EF Core (Npgsql provider), StackExchange.Redis, RabbitMQ.Client, OpenTelemetry.Extensions.Hosting (optional), Microsoft.Extensions.Logging, HealthChecks packages, NWebsec (optional)  
**Storage**: PostgreSQL (EF Core default; optional Dapper extension)  
**Testing**: xUnit + FluentAssertions (unit), integration via Testcontainers or docker-compose, custom lightweight fakes for adapters  
**Target Platform**: Linux container (amd64) / cross-platform dev; Aspire-ready orchestration  
**Project Type**: Server-side API template (single solution with layered projects)  
**Performance Goals**: P50 < 100ms / P95 < 400ms for typical read endpoints (local baseline); cold start readiness ≤ 5s  
**Constraints**: No paid packages, envelope metadata mandatory, domain layer free of infrastructure dependencies, problem+json error schema consistent, rate limiting globally enforced  
**Scale/Scope**: Template baseline for small-to-medium services; sample bounded context (Products) + scaffolding scripts  

All items resolved; no outstanding NEEDS CLARIFICATION markers.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Must satisfy BEFORE research start:
1. Architecture layering sketched (Domain, Application, Infrastructure, API) with no framework leakage inward.
2. All external dependencies listed with planned interfaces (`ICache`, `IMessageBus`, `IEventPublisher`, `IAuthService`, `IRepository<T>`, `IOutboxDispatcher`, `IFeatureFlagService`, `IResiliencePolicyProvider`).
3. Security baseline planned: JWT auth scopes, validation strategy, global rate limiting & CORS.
4. Observability skeleton defined: structured logging approach, health/readiness endpoints, metrics list, OpenTelemetry tracing coverage plan (≥90%).
5. Performance targets acknowledged (P50 <100ms, P95 <400ms typical read) with initial risk assessment.
6. Testing strategy drafted: domain/application unit coverage goals (≥95% handlers), integration adapter tests, contract tests against OpenAPI, validation pipeline test suite outline.
7. Documentation artifacts planned: README impact, ADR need, spec/plan/tasks traceability.
8. Event envelope fields confirmed (correlationId, causationId, tenantId, schemaVersion, timestamp).
9. Non-negotiables reviewed (no paid packages, centralized validation, no sensitive log data, outbox pattern for events, resilience policies, feature flag strategy, graceful shutdown sequence, versioning approach).
10. Resilience policy set defined (retry, circuit breaker, timeout) with initial thresholds.
11. Outbox dispatcher design chosen (polling interval, batching, retry strategy).
12. Feature flag evaluation mechanism & lifecycle documented.
13. Graceful shutdown procedure outlined (drain order: HTTP → consumers → outbox → connections).
14. Deployment artifacts list (Compose, Aspire manifests, Helm chart skeleton) enumerated.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
src/
├── Domain/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Events/
│   └── Policies/
├── Application/
│   ├── Interfaces/ (IRepository<T>, ICache, IMessageBus, IEventPublisher, IAuthService)
│   ├── DTOs/
│   ├── Validators/
│   ├── UseCases/
│   └── Services/
├── Infrastructure/
│   ├── Persistence.Postgres/
│   │   ├── DbContext/
│   │   ├── Migrations/
│   │   └── Repositories/
│   ├── Cache.Redis/
│   ├── Messaging.RabbitMq/
│   ├── Auth.Jwt/
│   └── Observability/
└── Api/
  ├── CompositionRoot/
  ├── Endpoints/ (Products, Health, Version)
  ├── Middleware/ (Validation, Correlation, ErrorHandling, RateLimiting)
  ├── Config/
  └── Swagger/

tests/
├── Unit/
│   ├── Domain/
│   └── Application/
├── Integration/
│   ├── Persistence/
│   ├── Cache/
│   ├── Messaging/
│   └── Api/
└── Smoke/

build/
├── docker-compose.yml
├── Dockerfile
└── scripts/

docs/
├── README.md
├── ADRs/
└── extending/
```

**Structure Decision**: Adopt layered solution with discrete folders per layer keeping domain pure, application orchestrating via interfaces, infrastructure segmented by capability for replaceability, and API hosting composition root + middleware. Testing separated by unit/integration/smoke to enforce isolation.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Repository pattern | Enables swap between EF Core and Dapper; abstraction of persistence boundary | Direct DB access couples application logic to storage and hinders replaceability |
| Message envelope abstraction | Guarantees consistent metadata for events | Ad-hoc events risk missing traceability fields |

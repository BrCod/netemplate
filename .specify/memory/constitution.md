# Clean Architecture .NET 8 API Template Constitution
<!-- Sync Impact Report: 1.0.0 → 1.1.0 | Modified: VI Observability (OTel mandatory), VIII Testing Discipline (added contract testing); Added Principles: X Versioning, XI Resilience, XII Reliability (Outbox), XIII Flexibility (Feature Flags), XIV Shutdown Safety, XV Deployment & Portability. Added Targets: OTel coverage, outbox drain reliability, contract test coverage. Templates Updated: plan-template.md ✅, tasks-template.md ✅, spec-template.md ✅ (no structural change needed), copilot-instructions.md ⚠ (OTel & resilience note to add). Follow-ups: Add resilience policy defaults doc (TODO(resilience-doc)). -->

## Core Principles

### I. Architecture (Domain-Centric Clean Architecture)
The solution MUST implement strict Clean Architecture layering: Domain isolated from Application isolated from Infrastructure isolated from Presentation. Frameworks MUST depend inward only via interfaces. No business logic in controllers. Composition root wires dependencies. Domain layer MUST have zero external package dependencies. Rationale: Prevent framework lock‑in, ensure testability and evolutionary design.
<!-- Example: Every feature starts as a standalone library; Libraries must be self-contained, independently testable, documented; Clear purpose required - no organizational-only libraries -->

### II. DI-First (Replaceable Dependencies)
All dependencies (database, cache, messaging, auth provider, logger, external clients) MUST be injected; NO static singletons. Abstractions (`ICache`, `IMessageBus`, `IEventPublisher`, `IAuthService`, `IRepository<T>`) MUST be constructor-injected. Rationale: Enables isolation, hot swapping, resilience.
<!-- Example: Every library exposes functionality via CLI; Text in/out protocol: stdin/args → stdout, errors → stderr; Support JSON + human-readable formats -->

### III. Event-Driven Boundaries
Domain, business, and integration events MUST use a unified envelope carrying `correlationId`, `causationId`, `tenantId`, `schemaVersion`, `timestamp`. Internal events in-process async; external events via RabbitMQ with durable queues and retries; handlers MUST be idempotent. Rationale: Traceability & scalable decoupling.
<!-- Example: TDD mandatory: Tests written → User approved → Tests fail → Then implement; Red-Green-Refactor cycle strictly enforced -->

### IV. Security (Zero Trust Defaults)
JWT authN/authZ with explicit scopes; global rate limiting & CORS; centralized validation before handlers (fail fast); no sensitive data in logs; quarterly OWASP review with 0 critical findings required before release. Rationale: Protect users & infrastructure.
<!-- Example: Focus areas requiring integration tests: New library contract tests, Contract changes, Inter-service communication, Shared schemas -->

### V. Performance & Efficiency
Read endpoints MUST meet P50 < 100ms, P95 < 400ms (local baseline). Pagination by default. Redis caching for hot reads (configurable TTL). PostgreSQL access MUST avoid N+1 (projections/explicit includes). Correlation IDs propagate through logs & envelopes. Rationale: Predictable performance & user experience.

### VI. Observability (Tracing & Metrics Mandatory)
Structured logging (JSON capable) with correlation/causation IDs; errors map to RFC 7807 problem+json; health/readiness/liveness endpoints container-friendly; metrics (latency, cache hit, retries, DB call durations, message publish latency) exported; OpenTelemetry tracing & metrics hooks MUST instrument all externally visible endpoints and adapter calls (≥90% coverage excluding trivial health checks). Rationale: Ensures distributed monitoring, root cause analysis, and SLO tracking.

### VII. Tooling & Dependencies
.NET 8; Aspire-ready; OpenAPI/Swagger; no paid/heavy packages (no MediatR—use internal pipeline). Code-first configuration; Docker-first builds reproducible locally. Rationale: Minimize cost & maximize portability.

### VIII. Testing Discipline & Contract Validation
Unit tests MUST cover domain + application handlers (≥95% handler coverage). Integration tests exercise infrastructure adapters (DB, cache, messaging). Contract tests MUST validate OpenAPI specification against runtime responses (request/response schema parity). Validation pipeline tests MUST assert problem+json for representative invalid cases. Deterministic CI (tests, static analysis, security). Lightweight hand-written test doubles only. Rationale: Guarantees behavioral fidelity and regression prevention.

### X. Versioning (Non-Breaking Evolution)
All public API endpoints MUST include explicit versioning (e.g., /api/v1) and any breaking change MUST introduce a new version while keeping previous active until deprecation period ends (≥1 minor release). Deprecations MUST be documented in changelog & OpenAPI with sunset headers when scheduled. Rationale: Preserves client stability and predictable evolution.

### XI. Resilience (Policies & Fault Tolerance)
External calls (DB, cache, messaging) MUST apply standardized resilience policies: retry (bounded with exponential backoff & jitter), circuit breaker (open on consecutive failures threshold), timeout, and bulkhead limits where applicable. Policies implemented via approved library (Polly) with configuration centralization; NO ad-hoc sleeps or infinite retries. Rationale: Maintains availability under transient faults.

### XII. Reliability (Transactional Outbox)
Event publishing MUST use an outbox pattern for DB-state + event consistency: write domain changes and outbox record in same transaction; background dispatcher MUST publish and mark as processed with idempotency. Failed publishes MUST retry with backoff until success or max attempts flagged for operator review. Rationale: Prevents lost or phantom events ensuring eventual consistency.

### XIII. Flexibility (Feature Flags)
Experimental or risky functionality MUST be guarded by feature flags evaluated at application boundary; flags MUST be reversible without redeploy; evaluation MUST be side-effect free. Flag lifecycle: propose → implement → experiment → adopt or remove (≤2 release cycles). Rationale: Enables safe progressive delivery.

### XIV. Shutdown Safety (Graceful Termination)
Application shutdown MUST initiate graceful stop: pause accepting new requests, drain in-flight requests (< configurable timeout), stop message consumers after acknowledging current messages, flush outbox dispatcher pending work, close DB and cache connections cleanly. Abrupt termination only allowed for critical security emergency. Rationale: Prevents data loss and partial processing.

### XV. Deployment & Portability
Template MUST include Docker Compose for local infra, Aspire-ready manifests, and Helm chart skeleton for Kubernetes deployment (values for image, resources, env). Deployment artifacts MUST reference health/readiness endpoints, expose metrics, and allow config of resilience policies & feature flags. Rationale: Facilitates frictionless environment promotion and portability across orchestrators.

### IX. Documentation & Traceability
README, contribution guide, ADRs for architecture & cross-cutting changes, maintained spec + plan + tasks artifacts per feature. API changes MUST be versioned & documented. Rationale: Shared understanding & faster onboarding.
<!-- Example: Text I/O ensures debuggability; Structured logging required; Or: MAJOR.MINOR.BUILD format; Or: Start simple, YAGNI principles -->

## Non-Negotiables & Explicit Interfaces
Non-negotiables: No paid packages; centralized composable validation; global rate limiting & CORS; message envelope metadata complete; outbox pattern mandatory for publish reliability; OpenTelemetry instrumentation mandatory (≥90% endpoint coverage); resilience policies (retry, circuit breaker, timeout) applied to all external adapter calls; feature flags reversible; no sensitive data in logs; domain purity (no infra deps). Interfaces: `ICache`, `IMessageBus`, `IEventPublisher`, `IAuthService`, `IRepository<T>`, plus `IOutboxDispatcher`, `IFeatureFlagService`, `IResiliencePolicyProvider` define boundaries and MUST be the only abstractions crossing infrastructure. Rationale: Codifies systemic guarantees.
<!-- Example: Technology stack requirements, compliance standards, deployment policies, etc. -->

## Performance, Security & Workflow Targets
Performance: P50 < 100ms, P95 < 400ms typical read. Security: 0 critical OWASP findings pre-release. Quality: ≥95% core handler coverage; ≥90% endpoint + adapter tracing coverage; ≥95% contract endpoints covered by contract tests; Outbox publish success rate ≥99.9% (failed items retried); Resilience circuit breaker false-open rate <1%. Workflow: PRs MUST include tests & doc updates; spec/plan changes MUST add/update ADR; API surface changes MUST bump version & update OpenAPI; version deprecations MUST list removal timeline; envelope schema changes MUST increment `schemaVersion` with migration notes; resilience & outbox metrics reviewed quarterly. Rationale: Enforces measurable reliability & evolvability.
<!-- Example: Code review requirements, testing gates, deployment approval process, etc. -->

## Governance
Amendments: ADR (Proposed) referencing affected principles → PR discussion → approval → ADR Accepted & version bump (MAJOR for removals/breaking, MINOR for additions/expansions, PATCH for clarifications). Compliance: Each PR MUST confirm Architecture layering, DI purity, Security baseline, Observability instrumentation, Test coverage delta, Documentation updates. Quarterly review validates principles & targets; gaps MUST yield remediation tasks within one sprint. Emergency security fixes MAY merge with retroactive ADR ≤48h. Maintainers MAY block merges violating non‑negotiables.
<!-- Example: All PRs/reviews must verify compliance; Complexity must be justified; Use [GUIDANCE_FILE] for runtime development guidance -->

**Version**: 1.1.0 | **Ratified**: 2025-11-13 | **Last Amended**: 2025-11-13
<!-- Example: Version: 2.1.1 | Ratified: 2025-06-13 | Last Amended: 2025-07-16 -->

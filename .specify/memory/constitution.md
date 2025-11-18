# Clean Architecture .NET 8 API Template Constitution
**Version**: 1.2.0 | **Ratified**: 2025-11-17 | **Status**: Active

## Core Principles

### I. Architecture
- Strict layering: Domain → Application → Infrastructure → Presentation.
- Domain events MUST be defined in the domain layer.
- Migrations MUST be idempotent and rollbackable.
- Controllers MUST contain no business logic.

### II. DI-First
- All dependencies MUST be injected; NO static singletons.
- Lifetime scope rules MUST be documented (transient vs scoped vs singleton).

### III. Event-Driven Boundaries
- Unified envelope with correlationId, causationId, tenantId, schemaVersion, timestamp.
- Schema registry MUST enforce version compatibility.
- Consumers MUST ignore unknown fields gracefully.

### IV. Security
- JWT auth with explicit scopes.
- Secrets MUST be managed via vault (Azure Key Vault, HashiCorp Vault).
- Quarterly OWASP review with 0 critical findings.

### V. Performance
- Read endpoints: P50 < 100ms, P95 < 400ms.
- Load testing thresholds MUST be validated in CI/CD.

### VI. Observability
- ≥90% OpenTelemetry coverage.
- Trace sampling strategy MUST be documented and configurable.

### VII. Dependencies
- New dependencies MUST be approved via ADR workflow.

### VIII. Testing
- ≥95% handler coverage.
- Mutation/property-based testing REQUIRED for critical domain logic.

### IX. Versioning
- Semantic versioning enforced across API + docs.
- Deprecations MUST include sunset headers.

### X. Resilience
- Retry, circuit breaker, timeout, bulkhead policies centralized in config files.

### XI. Reliability
- Outbox pattern mandatory.
- Dead-letter queue REQUIRED with operator alerting.

### XII. Feature Flags
- Audit logging of flag changes REQUIRED.
- Flags reversible without redeploy.

### XIII. Shutdown Safety
- Configurable shutdown timeout documented and validated.

### XIV. Deployment
- Environment parity testing REQUIRED (local vs staging vs prod).

### XV. Documentation
- Living architecture diagrams (C4 model) REQUIRED.

### XVI. Localization & Globalization
- Problem+json responses MUST be localized.
- Date/time, number formats MUST respect culture.
- OpenAPI docs MUST support multiple languages.
- Default fallback MUST be English if translation missing.

---

## Non-Negotiables
- No paid packages.
- Centralized validation.
- Outbox pattern mandatory.
- ≥90% tracing coverage.
- Resilience policies applied everywhere.
- Feature flags reversible.
- Domain purity (no infra deps).
- Automated compliance checks in CI/CD REQUIRED.

---

## Governance
- ADR-driven amendments.
- Compliance checks per PR.
- Quarterly review validates principles.
- Emergency fixes may merge with retroactive ADR ≤48h.
- Role-based responsibilities documented (maintainers vs contributors).

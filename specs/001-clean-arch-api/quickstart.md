
# Quickstart: Clean Architecture .NET 8 API Template

This guide walks new contributors through setup and incremental phases defined in the constitution, spec, and tasks.

---

## 1. Clone & Restore
```bash
git clone <repo-url>
cd netemplate
dotnet restore
dotnet build
```

---

## 2. Start Infrastructure
```bash
docker compose -f build/docker-compose.yml up -d
```
Starts Postgres, Redis, RabbitMQ.

---

## 3. Run API
```bash
dotnet run --project src/Api/Api.csproj
```
Visit Swagger: [https://localhost:5001/swagger](https://localhost:5001/swagger)

---

## 4. Phase 1 – Setup
- Create `src/`, `tests/`, `build/`, `docs/` folders.
- Add `.editorconfig`, `.gitignore`, `Directory.Build.props`.
- Initialize solution and projects (Api, Application, Domain, Infrastructure).

---

## 5. Phase 2 – Foundational
- Define base entities, value objects, domain events.
- Create interfaces (`IRepository<T>`, `ICache`, `IMessageBus`, `IEventPublisher`, `IAuthService`).
- Implement envelope contract with correlationId, causationId, tenantId, schemaVersion, timestamp.
- Setup DbContext, migrations (idempotent + rollbackable).
- Implement Redis cache, RabbitMQ publisher/subscriber, JWT validation.
- Register DI, add middleware (logging, validation, rate limiting, CORS, security headers).
- Add health checks, Swagger, structured logging, OpenTelemetry hooks.
- Register resilience policies (Polly).
- Implement outbox skeleton, feature flag service, graceful shutdown hooks.
- Add schema registry and trace sampling config.

---

## 6. Phase 3 – User Story 1 (Scaffolding)
- Scaffold sample `Products` context.
- Implement Product entity, repository, service, controller.
- Add validation, logging, pagination, caching.
- Write unit, integration, and contract tests.

---

## 7. Phase 4 – User Story 2 (Observability & Governance)
- Implement `/health/ready` aggregation.
- Ensure correlationId propagation in logs.
- Add config-driven rate limit and CORS policies.
- Integrate secrets vault (Azure Key Vault/HashiCorp Vault).
- Test health endpoints and log correlation.

---

## 8. Phase 5 – User Story 3 (CI Pipeline)
- Create Dockerfile and docker-compose.yml.
- Add build/test scripts.
- Configure GitHub Actions workflow.
- Add CI compliance checks (DI purity, resilience config, tracing coverage, contract validation).
- Add smoke and integration tests.
- Monitor error budget (≤0.1% failure rate).

---

## 9. Phase 6 – User Story 4 (Resilience & Outbox Reliability)
- Implement centralized resilience config (retry, circuit breaker, timeout, bulkhead).
- Add dead-letter queue handling + operator alerting.
- Write resilience fault injection tests.
- Test outbox dispatcher reliability under simulated failures.

---

## 10. Phase 7 – User Story 5 (Feature Flags & Versioning)
- Implement feature flag audit trail persistence.
- Enforce semantic versioning in CI.
- Add contract tests for concurrent API versions (`/api/v1`, `/api/v2`).

---

## 11. Phase 8 – User Story 6 (Localization & Globalization)
- Implement localization middleware for problem+json responses.
- Add culture-aware formatting (dates, numbers, currencies, time zones).
- Provide multilingual OpenAPI docs (English + French baseline).
- Add localization tests (English + French).
- Implement fallback to English if translation missing.
- Document localization/globalization strategy in ADR.

---

## 12. Final Phase – Polish & Governance
- Update README, contribution guide, replaceability guide.
- Add ADRs for key decisions (schema registry, localization).
- Add living architecture diagrams (C4 model).
- Run audits: trace coverage (≥90%), outbox reliability, contract test coverage (≥95%).
- Conduct quarterly governance review.

---

## Health & Readiness
- `/health/live` → process liveness.
- `/health/ready` → adapter readiness.

---

## Security Checklist
- JWT issuer/audience configured.
- Signing key rotation documented.
- Validation pre-handler active.
- No sensitive payload logging.
- Secure headers enabled (HSTS in production).

---

## Next Steps
- Use `spec.md` and `tasks.md` to track incremental progress.
- Each phase is independently testable and can be developed in parallel.
```


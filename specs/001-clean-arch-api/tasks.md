# Tasks: Clean Architecture .NET 8 API Template

**Input**: Epic and design docs from plan.md, spec.md

---

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 Create src/, tests/, build/, docs/ folders at repo root
- [X] T002 Add .editorconfig, .gitignore, Directory.Build.props to repo root
- [X] T003 [P] Initialize solution and projects: Api, Application, Domain, Infrastructure.Postgres, Infrastructure.Redis, Infrastructure.RabbitMq, Infrastructure.Auth
- [X] T004 [P] Wire common NuGet packages in all projects (no paid libraries)

---

## Phase 2: Foundational (Blocking Prerequisites)

- [X] T005 Setup base entities, value objects, domain events, result types in src/Domain/
- [X] T006 [P] Create interfaces: IRepository<T>, ICache, IMessageBus, IEventPublisher, IAuthService in src/Application/Interfaces/
- [X] T007 Implement message envelope contract (correlationId, causationId, tenantId, schemaVersion, timestamp) in src/Application/DTOs/Envelope.cs
- [X] T008 Add validation abstractions and base validators in src/Application/Validators/
- [X] T009 [P] Setup DbContext, migrations, repositories in src/Infrastructure/Postgres/ (idempotent + rollbackable migrations)
- [X] T010 [P] Implement cache-aside, key strategy, TTL policies in src/Infrastructure/Redis/
- [X] T011 [P] Implement RabbitMQ connection, publisher/subscriber, envelope serialization in src/Infrastructure/RabbitMq/
- [X] T012 [P] Configure JWT token validation (issuer, audience, keys) in src/Infrastructure/Auth/
- [X] T013 [P] Register DI for all interfaces and adapters in src/Api/Program.cs
- [X] T014 [P] Add request logging (correlation IDs), error handling (problem+json), validation pipeline, rate limiting, CORS, security headers in src/Api/Middleware/
- [X] T015 [P] Implement health checks (liveness/readiness) for infra adapters in src/Api/Health/
- [X] T016 [P] Add Swagger/OpenAPI with auth and response schemas in src/Api/Swagger/
- [X] T017 [P] Implement structured logging config with redaction, log scopes for correlationId in src/Api/Logging/
- [X] T018 [P] Add OpenTelemetry hooks, metrics, exporter config placeholders in src/Api/Observability/
- [X] T019 [P] Register retry policies (Polly) for infra adapters in src/Infrastructure/Policies/
- [X] T020 [P] Implement outbox table and dispatcher skeleton in src/Infrastructure/Postgres/Outbox/
- [X] T021 [P] Implement feature flag service abstraction and in-memory provider in src/Application/Services/FeatureFlags/
- [X] T022 [P] Implement graceful shutdown hooks in src/Api/Shutdown/
- [X] T023 [P] Enforce HTTPS, HSTS, secure headers, strict CORS in src/Api/Security/
- [X] T024 [P] Implement input size limits, safe deserialization, model binding limits in src/Api/Security/
- [X] T025 [P] Implement rate limiting policies and 429 responses in src/Api/Middleware/RateLimiting/
- [X] T026 [P] Add logging redaction and PII guardrails in src/Api/Logging/
- [X] T027 [P] Implement schema registry contract for message envelopes in src/Application/Messaging/SchemaRegistry/
- [X] T028 [P] Add trace sampling configuration in src/Api/Observability/Config/

---

## Phase 3: User Story 1 - Rapid Bounded Context Scaffolding (Priority: P1) [US1]

**Goal**: Enable rapid creation of new bounded contexts with all conventions pre-wired  
**Independent Test**: Scaffold sample context, verify validation, auth, logging

- [X] T029 [P] [US1] Scaffold sample "Products" context: Domain, Application, Infrastructure, API folders in src/Products/
- [X] T030 [P] [US1] Create Product entity, value object, domain event in src/Products/Domain/
- [X] T031 [P] [US1] Implement ProductRepository in src/Products/Infrastructure/Postgres/
- [X] T032 [P] [US1] Implement ProductService in src/Products/Application/Services/
- [X] T033 [P] [US1] Add ProductController with pagination and caching in src/Products/Api/
- [X] T034 [US1] Add validation and error handling for Product endpoints in src/Products/Api/
- [X] T035 [US1] Add logging for Product operations in src/Products/Api/
- [X] T036 [US1] Add unit tests for Product domain, validators, service in tests/Products/Unit/
- [X] T037 [US1] Add integration tests for ProductRepository, ProductService in tests/Products/Integration/
- [X] T038 [US1] Add contract test for Product endpoint in tests/Products/Contract/

---

## Phase 4: User Story 2 - Operational Observability & Governance (Priority: P2) [US2]

**Goal**: Enable health/readiness, log correlation, config-driven rate limits/CORS, secret rotation  
**Independent Test**: Deploy locally, hit health endpoints, adjust config, verify changes

- [X] T039 [P] [US2] Implement /health/ready endpoint aggregation in src/Api/Health/
- [X] T040 [P] [US2] Implement log correlationId propagation in src/Api/Logging/
- [X] T041 [P] [US2] Add config-driven rate limit and CORS policies in src/Api/Middleware/
- [X] T042 [P] [US2] Implement secret rotation config in src/Api/Security/
- [X] T043 [P] [US2] Integrate secrets vault (Azure Key Vault/HashiCorp Vault) in src/Infrastructure/Security/
- [X] T044 [US2] Add integration tests for health/readiness endpoints in tests/Api/Integration/
- [X] T045 [US2] Add contract test for log correlation in tests/Api/Contract/

---

## Phase 5: User Story 3 - Automated CI Validation Pipeline (Priority: P3) [US3]

**Goal**: Automate build, test, health-check, smoke validation in CI  
**Independent Test**: Run CI script, verify all stages pass deterministically

- [ ] T046 [P] [US3] Create Dockerfile for API in build/
- [ ] T047 [P] [US3] Create docker-compose.yml with Postgres, Redis, RabbitMQ in build/ - NOT NEEDED FOR NOW, existing services will be used
- [X] T048 [P] [US3] Add Makefile/PowerShell scripts for build/test/run/migrate/seed in build/
- [X] T049 [P] [US3] Add GitHub Actions workflow for CI build, tests, lint, artifact publish in .github/workflows/
- [X] T050 [P] [US3] Add CI compliance checks: DI purity, resilience config, tracing coverage, contract validation in .github/workflows/compliance.yml
- [X] T051 [US3] Add smoke tests for health endpoints, JWT-protected endpoint in tests/Api/Smoke/
- [ ] T052 [US3] Add integration tests for infra adapters in tests/Api/Integration/
- [ ] T053 [US3] Add error budget monitoring (≤0.1% failure rate) in CI metrics validation step

---

## Phase 6: User Story 4 - Resilience & Outbox Reliability (Priority: P4) [US4]

**Goal**: Validate retry/backoff, circuit breaker, and outbox dispatcher reliability under transient faults  
**Independent Test**: Inject failures; confirm retries, circuit breaker trips, and outbox publishes succeed or DLQ captures failures

- [X] T054 [P] [US4] Implement centralized resilience config (retry, circuit breaker, timeout, bulkhead) in src/Infrastructure/Policies/Config/
- [X] T055 [P] [US4] Add dead-letter queue handling + operator alerting in src/Infrastructure/RabbitMq/DeadLetter/
- [X] T056 [US4] Add resilience fault injection tests in tests/Infrastructure/Resilience/
- [X] T057 [US4] Add outbox dispatcher reliability tests with simulated failures in tests/Infrastructure/Postgres/Outbox/

---

## Phase 7: User Story 5 - Feature Flags & Versioning (Priority: P5) [US5]

**Goal**: Toggle feature flags without redeploy and validate API versioning strategy  
**Independent Test**: Toggle flag in config; confirm behavior changes within 60s; call /api/v1 and /api/v2 concurrently

- [X] T058 [P] [US5] Implement feature flag audit trail persistence in src/Infrastructure/FeatureFlags/
- [ ] T059 [P] [US5] Add semantic versioning enforcement in CI pipeline (.github/workflows/versioning.yml)
- [X] T060 [US5] Add contract tests for concurrent API versions (/api/v1, /api/v2) in tests/Api/Contract/Versioning/

---
## Phase 8: User Story 6 - Localization & Globalization (Priority: P6) [US6]

**Goal**: Ensure the API supports multiple languages, cultures, and time zones for error messages, validation responses, and documentation.  
**Independent Test**: Configure locale to `fr-CA`; send invalid payload; confirm problem+json response localized in French.

- [X] T061 [P] [US6] Implement localization middleware for problem+json responses in src/Api/Middleware/Localization/
- [X] T062 [P] [US6] Add culture-aware formatting (dates, numbers, currencies, time zones) in src/Application/Localization/Formatters/
- [X] T063 [P] [US6] Add multilingual OpenAPI documentation (English + Turkish baseline) in src/Api/Swagger/Localization/
- [X] T064 [US6] Add localization tests (English + Turkish) in tests/Api/Localization/
- [X] T065 [US6] Implement fallback strategy to default language (English) if translation missing
- [ ] T066 [US6] Add governance ADR documenting localization/globalization strategy in docs/adr/ADR-localization.md

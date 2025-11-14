# Tasks: Clean Architecture .NET 8 API Template

**Input**: Epic and design docs from plan.md, spec.md

---

## Phase 1: Setup (Shared Infrastructure)

- [ ] T001 Create src/, tests/, build/, docs/ folders at repo root
- [ ] T002 Add .editorconfig, .gitignore, Directory.Build.props to repo root
- [ ] T003 [P] Initialize solution and projects: Api, Application, Domain, Infrastructure.Postgres, Infrastructure.Redis, Infrastructure.RabbitMq, Infrastructure.Auth
- [ ] T004 [P] Wire common NuGet packages in all projects (no paid libraries)

---

## Phase 2: Foundational (Blocking Prerequisites)

- [ ] T005 Setup base entities, value objects, domain events, result types in src/Domain/
- [ ] T006 [P] Create interfaces: IRepository<T>, ICache, IMessageBus, IEventPublisher, IAuthService in src/Application/Interfaces/
- [ ] T007 Implement message envelope contract (correlationId, causationId, tenantId, schemaVersion, timestamp) in src/Application/DTOs/Envelope.cs
- [ ] T008 Add validation abstractions and base validators in src/Application/Validators/
- [ ] T009 [P] Setup DbContext, migrations, repositories in src/Infrastructure/Postgres/
- [ ] T010 [P] Implement cache-aside, key strategy, TTL policies in src/Infrastructure/Redis/
- [ ] T011 [P] Implement RabbitMQ connection, publisher/subscriber, envelope serialization in src/Infrastructure/RabbitMq/
- [ ] T012 [P] Configure JWT token validation (issuer, audience, keys) in src/Infrastructure/Auth/
- [ ] T013 [P] Register DI for all interfaces and adapters in src/Api/Program.cs
- [ ] T014 [P] Add request logging (correlation IDs), error handling (problem+json), validation pipeline, rate limiting, CORS, security headers in src/Api/Middleware/
- [ ] T015 [P] Implement health checks (liveness/readiness) for infra adapters in src/Api/Health/
- [ ] T016 [P] Add Swagger/OpenAPI with auth and response schemas in src/Api/Swagger/
- [ ] T017 [P] Implement structured logging config with redaction, log scopes for correlationId in src/Api/Logging/
- [ ] T018 [P] Add OpenTelemetry hooks, metrics, exporter config placeholders in src/Api/Observability/
- [ ] T019 [P] Register retry policies (Polly) for infra adapters in src/Infrastructure/Policies/
- [ ] T020 [P] Implement outbox table and dispatcher skeleton in src/Infrastructure/Postgres/Outbox/
- [ ] T021 [P] Implement feature flag service abstraction and in-memory provider in src/Application/Services/FeatureFlags/
- [ ] T022 [P] Implement graceful shutdown hooks in src/Api/Shutdown/
- [ ] T023 [P] Enforce HTTPS, HSTS, secure headers, strict CORS in src/Api/Security/
- [ ] T024 [P] Implement input size limits, safe deserialization, model binding limits in src/Api/Security/
- [ ] T025 [P] Implement rate limiting policies and 429 responses in src/Api/Middleware/RateLimiting/
- [ ] T026 [P] Add logging redaction and PII guardrails in src/Api/Logging/

---

## Phase 3: User Story 1 - Rapid Bounded Context Scaffolding (Priority: P1) [US1]

**Goal**: Enable rapid creation of new bounded contexts with all conventions pre-wired
**Independent Test**: Scaffold sample context, verify validation, auth, logging

- [ ] T027 [P] [US1] Scaffold sample "Products" context: Domain, Application, Infrastructure, API folders in src/Products/
- [ ] T028 [P] [US1] Create Product entity, value object, domain event in src/Products/Domain/
- [ ] T029 [P] [US1] Implement ProductRepository in src/Products/Infrastructure/Postgres/
- [ ] T030 [P] [US1] Implement ProductService in src/Products/Application/Services/
- [ ] T031 [P] [US1] Add ProductController with pagination and caching in src/Products/Api/
- [ ] T032 [US1] Add validation and error handling for Product endpoints in src/Products/Api/
- [ ] T033 [US1] Add logging for Product operations in src/Products/Api/
- [ ] T034 [US1] Add unit tests for Product domain, validators, service in tests/Products/Unit/
- [ ] T035 [US1] Add integration tests for ProductRepository, ProductService in tests/Products/Integration/
- [ ] T036 [US1] Add contract test for Product endpoint in tests/Products/Contract/

---

## Phase 4: User Story 2 - Operational Observability & Governance (Priority: P2) [US2]

**Goal**: Enable health/readiness, log correlation, config-driven rate limits/CORS, secret rotation
**Independent Test**: Deploy locally, hit health endpoints, adjust config, verify changes

- [ ] T037 [P] [US2] Implement /health/ready endpoint aggregation in src/Api/Health/
- [ ] T038 [P] [US2] Implement log correlationId propagation in src/Api/Logging/
- [ ] T039 [P] [US2] Add config-driven rate limit and CORS policies in src/Api/Middleware/
- [ ] T040 [P] [US2] Implement secret rotation config in src/Api/Security/
- [ ] T041 [US2] Add integration tests for health/readiness endpoints in tests/Api/Integration/
- [ ] T042 [US2] Add contract test for log correlation in tests/Api/Contract/

---

## Phase 5: User Story 3 - Automated CI Validation Pipeline (Priority: P3) [US3]

**Goal**: Automate build, test, health-check, smoke validation in CI
**Independent Test**: Run CI script, verify all stages pass deterministically

- [ ] T043 [P] [US3] Create Dockerfile for API in build/
- [ ] T044 [P] [US3] Create docker-compose.yml with Postgres, Redis, RabbitMQ in build/
- [ ] T045 [P] [US3] Add Makefile/PowerShell scripts for build/test/run/migrate/seed in build/
- [ ] T046 [P] [US3] Add GitHub Actions workflow for CI build, tests, lint, artifact publish in .github/workflows/
- [ ] T047 [US3] Add smoke tests for health endpoints, JWT-protected endpoint in tests/Api/Smoke/
- [ ] T048 [US3] Add integration tests for infra adapters in tests/Api/Integration/

---

## Final Phase: Polish & Cross-Cutting Concerns

- [ ] T049 [P] Documentation updates: README, replaceability guide, contribution guide in docs/
- [ ] T050 [P] Add ADRs for key decisions in docs/adr/
- [ ] T051 Code cleanup and refactoring across src/
- [ ] T052 Performance optimization for endpoints in src/Api/
- [ ] T053 [P] Additional unit tests in tests/
- [ ] T054 Security hardening audit in src/Api/Security/
- [ ] T055 Run quickstart.md validation in specs/001-clean-arch-api/quickstart.md
- [ ] T056 Trace coverage audit (ensure ≥90%) in src/Api/Observability/
- [ ] T057 Outbox reliability audit & metrics review in src/Infrastructure/Postgres/Outbox/
- [ ] T058 Contract test coverage audit (ensure ≥95% endpoints) in tests/Api/Contract/

---

## Dependencies & Execution Order

- Setup (Phase 1): No dependencies
- Foundational (Phase 2): Depends on Setup completion
- User Stories (Phase 3+): Depend on Foundational phase completion; can proceed in parallel
- Polish (Final Phase): Depends on all user stories being complete

## Parallel Execution Examples

- All [P] tasks in Setup and Foundational phases can run in parallel
- All user stories can be implemented in parallel after Foundational phase
- All [P] tests and models within a user story can run in parallel

## Implementation Strategy

- MVP: Complete Setup, Foundational, and User Story 1 ([US1])
- Incremental: Add User Story 2 ([US2]), then User Story 3 ([US3]), each independently testable
- Parallel: Multiple developers can work on different user stories after Foundational phase


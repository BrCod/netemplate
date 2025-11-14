# Data Model: Clean Architecture API Template

**Branch**: 001-clean-arch-api  
**Generated**: 2025-11-13  
**Source**: spec.md & research.md

## Entities

### Product
Fields:
- Id (GUID)
- Name (string, 3-200 chars)
- Description (string, optional, max 1000 chars)
- Price (decimal >= 0, scale 2)
- CreatedAt (UTC DateTime)
- UpdatedAt (UTC DateTime)
- IsActive (bool)
Validation Rules:
- Name required, trimmed, unique per tenant
- Price non-negative
- Description sanitized (output encoding)
State Transitions:
- Create → Active
- Update → Active (UpdatedAt refresh)
- Deactivate → IsActive=false (events emitted)

### MessageEnvelope
Fields:
- CorrelationId (GUID) REQUIRED
- CausationId (GUID nullable) OPTIONAL
- TenantId (string nullable)
- SchemaVersion (int)
- Timestamp (UTC DateTime)
- EventType (string)
- Payload (opaque object / serialized JSON)
Validation Rules:
- CorrelationId always present
- SchemaVersion positive
- Timestamp within acceptable clock skew (<5 minutes future)

### HealthProbe
Fields:
- Name (string)
- Status (enum: Healthy, Degraded, Unhealthy)
- DurationMs (int)
- Diagnostics (string key/value serialized)
Validation:
- Name required
- DurationMs >= 0

### RateLimitPolicy
Fields:
- PolicyName (string)
- Window (TimeSpan)
- PermitLimit (int)
- QueueLimit (int optional)
- BurstLimit (int optional)
Validation:
- PermitLimit > 0
- Window > 0

### DomainEvent (abstract)
Fields:
- Id (GUID)
- OccurredAt (UTC DateTime)
- Envelope (MessageEnvelope reference)
Subclasses:
- ProductCreatedEvent
- ProductUpdatedEvent
- ProductDeactivatedEvent

## Relationships
- Product emits DomainEvents wrapped by MessageEnvelope.
- HealthProbe collection aggregates into readiness response.
- RateLimitPolicy applied per endpoint grouping.

## Value Objects
### Money
Fields: Amount (decimal), Currency (ISO code)
Validation: Amount >= 0; Currency in supported list

### ProductName
Fields: Value (string)
Validation: 3-200 chars, no control characters

## Aggregates
- Product Aggregate Root: manages Product state transitions and emits events.

## Repository Contracts
- IRepository<Product>: GetById, List (paged), Add, Update, Delete (soft deactivate), SaveChanges (transaction boundary outside domain).

## Consistency & Invariants
- Product uniqueness enforced at repository/data layer (Name per tenant).
- Domain events raised before persistence commit; publishing after successful commit.

## Data Access Patterns
- EF Core for CRUD with projection to DTOs for API responses.
- Dapper optional performance path for read queries (optimized selects).

## Caching Interaction
- Cache key pattern: `tenant:products:{id}` and `tenant:products:list:{page}:{filter}`
- Invalidate on Product update/deactivation.

## Security Considerations
- No PII fields in Product entity.
- Envelope metadata excludes secrets.

## Open Questions
None.

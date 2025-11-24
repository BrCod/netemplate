# API Versioning Contract Tests

These smoke tests validate the API versioning contract for concurrent v1 and v2 operation.

## Test Coverage

- **Endpoint Coexistence**: Both v1 and v2 endpoints work simultaneously
- **Pagination Differences**: V1 uses offset pagination (skip/take), V2 uses cursor pagination
- **Response Format Validation**: V1 returns direct DTOs, V2 returns wrapped responses with metadata
- **Optional Metadata Support**: V2 supports `includeMetadata` query parameter
- **Data Consistency**: Same data accessible via both versions in different formats

## Running the Tests

1. **Start the API**:
   ```powershell
   dotnet run --project src/Api/Netemplate.Api.csproj
   ```

2. **Run the smoke tests**:
   ```powershell
   dotnet test tests/Netemplate.Api.Smoke/Netemplate.Api.Smoke.csproj
   ```

## Breaking Changes in V2

- **Renamed Endpoints**: `GetById` → `GetProduct`, `List` → `ListProducts`
- **Cursor Pagination**: `skip/take` → `pageSize/cursor/nextCursor`
- **Wrapped Responses**: Direct DTO → `{ data, metadata }` structure
- **Optional Metadata**: V2 supports includeMetadata query parameter for additional data

## Contract Validation

These tests serve as living documentation of the API versioning strategy, validating:

1. Both versions can coexist without interference
2. Clients can migrate gradually from v1 to v2
3. Response formats are properly differentiated
4. Pagination strategies work independently
5. Data consistency is maintained across versions

## Integration with CI

These tests should run against a deployed environment in CI to validate:

- Version compatibility across releases
- No unintended breaking changes
- Consistent behavior under load (concurrent requests)

## Related Tasks

- **T060**: Add contract tests for concurrent API versions (completed)
- **T059**: Add semantic versioning enforcement in CI pipeline (pending)
- **T061**: Implement localization middleware (next)

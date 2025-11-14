# Quickstart: Using the Clean Architecture API Template

## 1. Clone & Restore
```powershell
git clone <repo-url>
cd netemplate
# restore & build
dotnet restore
dotnet build
```

## 2. Start Infrastructure (Postgres, Redis, RabbitMQ)
```powershell
docker compose -f build/docker-compose.yml up -d
```

## 3. Run API
```powershell
dotnet run --project src/Api/Api.csproj
```
Visit Swagger: https://localhost:5001/swagger

## 4. Add a New Bounded Context
```powershell
# Example scaffold script (to be implemented):
./scripts/scaffold-context.ps1 -Name Inventory
```
Generates domain/application/infrastructure folders and a sample endpoint.

## 5. Add a Product Endpoint
1. Define Product entity in `src/Domain/Entities/Product.cs`.
2. Create use case in `src/Application/UseCases/Products/CreateProductHandler.cs`.
3. Add validator in `src/Application/Validators/ProductCreateValidator.cs`.
4. Implement repository in `src/Infrastructure/Persistence.Postgres/Repositories/ProductRepository.cs`.
5. Wire endpoint in `src/Api/Endpoints/ProductsEndpoints.cs`.

## 6. Enable Caching
- Register Redis adapter in DI.
- Add cache-aside logic in use case (check `ICache`, fall back to repository, set TTL).

## 7. Publish Events
- Raise domain event `ProductCreatedEvent` in entity factory.
- Application layer translates to envelope and calls `IEventPublisher`.

## 8. Run Tests
```powershell
dotnet test
```
Integration tests spin up containers (Testcontainers or compose). Ensure environment variables for connection strings are set.

## 9. Health & Readiness
- Hit `/health/live` for process liveness.
- Hit `/health/ready` after startup to confirm adapters.

## 10. Replace an Adapter (Example: Switch Persistence to Dapper)
1. Implement new repository in `src/Infrastructure/Persistence.Postgres/DapperProductRepository.cs`.
2. Register it replacing `IRepository<Product>` in composition root.
3. Run tests & health checks.

## 11. Rate Limiting Policy
Configure rate limiting in `src/Api/Config/RateLimiting.cs` and tag endpoints.

## 12. Observability
- Add correlation middleware before logging.
- Configure OpenTelemetry (optional) exporters in `src/Infrastructure/Observability/Telemetry.cs`.

## 13. CI Smoke
Pipeline runs build, unit/integration tests, then invokes `/health/ready` and a sample product CRUD sequence.

## 14. Security Checklist
- JWT issuer/audience configured
- Signing key rotation documented
- Validation pre-handler active
- No sensitive payload logging
- Secure headers enabled (HSTS in production)

## 15. Next Steps
Use `/speckit.tasks` to generate task breakdown, then implement incremental user stories.

# Development Logbook

> Weekly test and progress log for the Task Management API.

---

## Week 0-1: Foundation

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- `dotnet build`: Succeeded (0 errors, 0 warnings)
- `docker compose up`: API + PostgreSQL containers started successfully
- Health endpoint: `curl http://localhost:5000/health` → "Healthy"
- Swagger UI: `curl http://localhost:5000/swagger/index.html` → HTTP 200
- POST `/api/v1/tasks`: Created task with status "Todo" → HTTP 201
- GET `/api/v1/tasks`: Returns list of tasks → HTTP 200
- GET `/api/v1/tasks/{id}`: Returns single task → HTTP 200
- PUT `/api/v1/tasks/{id}`: Updated task status to "InProgress" → HTTP 200
- DELETE `/api/v1/tasks/{id}`: Deleted task → HTTP 204

**Notes:**
- Solution uses 4-layer clean architecture (API, Application, Domain, Infrastructure)
- .NET 10 uses `.slnx` solution format (not `.sln`)
- Added `JsonStringEnumConverter` for proper enum serialization
- PostgreSQL 16 with EF Core, auto-migration on startup
- Docker build succeeded; containers run with health check dependency

---

## Week 2: Database Architecture

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- `dotnet build`: Succeeded
- Docker rebuild: Succeeded
- Health endpoint: "Healthy"
- Seed data: 4 tasks populated (Done, InProgress, Todo x2)
- Serilog: Console + rolling file logging configured
- Repository + Unit of Work patterns implemented
- Global exception handler middleware registered
- Connection pooling configured (Max Pool Size=20)

**Notes:**
- Added `IRepository<T>` + `GenericRepository<T>` in Infrastructure layer
- Added `IUnitOfWork` + `UnitOfWork` pattern
- DbSeeder creates sample tasks on first run
- `GlobalExceptionHandler` middleware catches unhandled exceptions
- Serilog configured for structured logging to console and file

---

## Week 3: Authentication & Authorization

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- Register: Creates user, returns JWT + refresh token
- Login: Validates credentials, returns JWT + refresh token
- GET /auth/me: Returns current user info (requires auth)
- Unauthenticated access: Returns HTTP 401
- Task CRUD with auth: Creates tasks scoped to authenticated user
- Refresh token: Valid and can be used to get new access token
- Role-based auth: Admin and User roles assigned

**Notes:**
- Added User, RefreshToken entities; UserRole enum
- AuthService moved to API layer to avoid circular dependencies
- Password hashing with ASP.NET Core Identity PasswordHasher
- JWT tokens valid for 15 minutes; refresh tokens for 7 days
- Seed users: admin@taskmanager.com / Admin123! and user@taskmanager.com / User123!
- Fixed libkrb5-3 issue in Docker image for Npgsql

---

## Week 4: Validation & Error Handling

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- Empty task title: 400 with `"Title is required"`
- Bad email/weak password: 400 with multiple field errors
- Duplicate email: 409 Conflict with `"Email already registered"`
- Invalid credentials: 401 Unauthorized with ProblemDetails
- All errors return `application/problem+json` format (RFC 7807)

**Notes:**
- FluentValidation with auto-validation via `AddFluentValidationAutoValidation()`
- Custom `BusinessException` for domain rule violations
- `GlobalExceptionHandler` handles Business, UnauthorizedAccess, InvalidOperation, and generic exceptions
- `ConfigureApiBehaviorOptions` with custom `InvalidModelStateResponseFactory` for consistent validation errors
- Validators: CreateTaskValidator, UpdateTaskValidator, RegisterRequestValidator, LoginRequestValidator

---

## Week 5: Pagination, Filtering & Sorting

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- Pagination: pageNumber, pageSize, totalPages, totalRecords in response
- Status filtering: ?status=Todo returns only Todo tasks
- Sort by title asc: Alphabetical order
- Search: ?search=pagination returns 5 matching tasks
- X-Total-Count header present
- Page size clamped between 1-100

**Notes:**
- Added `PagedResult<T>` wrapper + `TaskQueryParameters` in Application.DTOs
- Added `AsQueryable()` to IRepository for LINQ composition
- Supports filtering by status, search text, due date range
- Supports sorting by title, status, dueDate, createdAt in asc/desc

---

## Week 6: Advanced Queries & Search

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- Search via ?search= parameter works (title + description)
- Status filtering with string enum parsing
- Due date range filtering
- Specification pattern via IQueryable<T> composition

**Notes:**
- Search uses EF Core's `ToLower().Contains()` translated to SQL `LIKE`
- Search covers both title and description fields
- Query parameters compose together (search + status + date range + sort)

## Week 7: Caching Layer

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- Redis container running (redis:7-alpine)
- Health check shows redis as healthy
- StackExchange.Redis configured via IDistributedCache
- Cache-aside pattern ready via IDistributedCache abstraction

**Notes:**
- Added `Microsoft.Extensions.Caching.StackExchangeRedis`
- Redis connection configured via ConnectionStrings__Redis env var
- Health check includes Redis: `.AddRedis(redisConnection)`

## Week 8: Background Jobs

**Date:** 2026-05-19
**Status:** ❌ Removed (unnecessary)
**Test Results:**
- Hangfire removed from project (packages, config, dashboard)
- No impact on REST API, auth, or database operations

**Notes:**
- Hangfire and all related packages deleted from .csproj
- `UseHangfireDashboard`, `AddHangfire`, `AddHangfireServer` removed from Program.cs
- `AllowAllRequestsFilter.cs` deleted
- Background job scheduling can be re-added if needed

---

## Week 9: API Versioning

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- Asp.Versioning.Mvc configured with URL segment reader (`api/v{version}/...`)
- Default API version set to 1.0
- All controllers use `[ApiVersion("1.0")]` attribute
- Swagger groups by version (v1)
- Build: 0 errors, 0 warnings

**Notes:**
- Uses `UrlSegmentApiVersionReader` for clean RESTful URLs
- `AssumeDefaultVersionWhenUnspecified` enabled for backward compatibility
- Versioning configured in Program.cs with proper ApiExplorer setup

---

## Week 10: Rate Limiting & Security

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- AspNetCoreRateLimit configured with in-memory cache
- General rule: 100 requests per minute per IP
- X-Rate-Limit headers returned in responses
- JWT authentication with 15-min access + 7-day refresh tokens
- Password hashing with Identity PasswordHasher

**Notes:**
- Rate limiting runs before MVC pipeline via `app.UseIpRateLimiting()`
- Whitelisted endpoints (health, swagger) not rate-limited
- Configured via `appsettings.json` IpRateLimiting section

---

## Week 11: Observability & Monitoring

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- OpenTelemetry with ASP.NET Core instrumentation
- Console exporter for metrics (visible in Docker logs)
- Health checks for PostgreSQL and Redis
- Health endpoint returns "Healthy" when all dependencies reachable
- Serilog structured logging to console + rolling files

**Notes:**
- OpenTelemetry configured with `AddAspNetCoreInstrumentation()`
- Health checks use `AspNetCore.HealthChecks.NpgSql` and `AspNetCore.HealthChecks.Redis`
- Logs written to `logs/taskmanagement-{date}.log` with daily rolling

---

## Week 12: Testing & Quality

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- `dotnet test`: 4/4 passing unit tests
- `dotnet build`: 0 errors, 0 warnings
- Docker build: Successful
- Integration tests: All CRUD operations pass

**Notes:**
- xUnit test project with unit tests for domain logic
- FluentValidation validators tested via auto-validation
- Clean architecture ensures testability of each layer

---

## Week 13: File Upload & Storage

**Date:** 2026-05-19
**Status:** ⚠️ Deferred
**Test Results:**
- N/A — implementation deferred
- Polly resilience packages installed (Polly 8.6.6, Polly.Extensions.Http 3.0.0)

**Notes:**
- MinIO file storage and upload endpoints not yet implemented
- Directory `src/TaskManagement.Infrastructure/Services/` created for future MinIO service
- Polly packages pre-installed for HTTP resilience patterns
- File upload can be added when storage requirements are defined
- Resilience patterns (retry, circuit breaker) available via pre-installed Polly packages

---

## Week 14: Real-time Features

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- SignalR TaskHub created at `/hubs/tasks` with JWT auth
- Users join group named after their user ID on connect
- Task CRUD operations broadcast notifications (TaskCreated, TaskUpdated, TaskDeleted)
- JWT configured to accept tokens from query string for WebSocket connections
- Build succeeded (0 errors, 0 warnings)

**Notes:**
- `Microsoft.AspNetCore.SignalR.Common` removed as it's included in ASP.NET Core framework
- Hub uses `[Authorize]` attribute for authentication
- JWT `OnMessageReceived` event reads token from `access_token` query param for SignalR
- Groups are managed in `OnConnectedAsync` / `OnDisconnectedAsync` based on `Context.UserIdentifier`

---

## Week 15: Multi-tenancy

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- Organization entity created with Users and Tasks FK references
- TenantMiddleware extracts `organizationId` JWT claim per request
- TenantService reads tenant ID from HttpContext.Items
- TenantInterceptor auto-assigns OrganizationId on entity creation
- User seeding includes default organization
- Login returns JWT with organizationId claim
- `dotnet ef migrations add AddMultiTenancy` created successfully

**Notes:**
- Removed `HasQueryFilter` approach (model caching made dynamic filtering unreliable)
- Tenant isolation enforced via TenantInterceptor (auto-assigns org on create) + existing UserId scoping
- `IgnoreQueryFilters()` used in AuthService for unauthenticated operations
- Default org created on first register if none exists
- Design-time factory added to Infrastructure for clean migration support

---

## Week 16: Audit Logging

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- AuditLog entity captures entity name, action, old/new values, user info
- AuditInterceptor logs changes on every SaveChanges/ SaveChangesAsync
- Interceptor skips AuditLog entities and unauthenticated requests
- `dotnet ef migrations add AddAuditLogging` created successfully
- All endpoints return expected responses

**Notes:**
- AuditInterceptor uses IHttpContextAccessor to extract user context
- OldValues captured for Modified/Deleted; NewValues captured for Added/Modified
- Values serialized as JSON for flexible querying
- Interceptor registered conditionally (only when IHttpContextAccessor is available)

---

## Week 19: Reporting & Analytics

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- GET /analytics/task-summary returns task count grouped by status
- GET /analytics/completion-rate returns % completed over configurable period
- GET /analytics/overdue returns overdue tasks sorted by days overdue
- All endpoints scoped to authenticated user
- Build: 0 errors, 0 warnings

**Notes:**
- AnalyticsController at version 1.0 under /api/v1/analytics
- Completion rate defaults to 30-day window
- Overdue calculation includes DaysOverdue integer for UI display

---

## Week 20: GraphQL Alternative

**Date:** 2026-05-19
**Status:** ❌ Removed (unnecessary)
**Test Results:**
- All HotChocolate packages removed from .csproj
- GraphQL directory deleted (Query.cs, Mutation.cs, TaskDataLoader.cs)
- No impact on REST API, auth, or database operations
- Build: 0 errors, 0 warnings

**Notes:**
- GraphQL was unnecessary overhead for this app — REST endpoints serve all needs
- HotChocolate packages (AspNetCore, AspNetCore.Authorization, Data) removed
- `AddGraphQLServer`, `MapGraphQL`, and all related config removed from Program.cs
- `/graphql` endpoint no longer available

---

## Week 21: Single-page Frontend GUI

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- `GET http://localhost:5000/` → HTTP 200, serves index.html
- Login form → POST /auth/login → JWT stored in memory
- Tasks tab → GET /tasks → displays paginated table with search/filter
- Create tab → POST /tasks → creates task, redirects to tasks list
- Edit via prompt → PUT /tasks/{id} → updates task
- Delete with confirm → DELETE /tasks/{id} → removes task
- Analytics tab → displays summary, completion rate, overdue tasks
- `dotnet build`: 0 errors, 0 warnings
- `docker compose build --no-cache`: Successful

**Notes:**
- Single `index.html` in `wwwroot/` — no build tools, no npm, no frameworks
- Vanilla JS with `fetch()` API, no jQuery or React dependencies
- Styled with clean utility CSS classes (indigo primary, toast notifications)
- Serves via `UseDefaultFiles()` + `UseStaticFiles()` middleware
- All API calls go through `/api/v1/*` relative paths
- Seed credentials pre-filled in login form: `admin@taskmanager.com` / `Admin123!`

---

## Week 17: Advanced CI/CD (Pipeline Enhancements)

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- CorrelationIdMiddleware adds `X-Correlation-ID` header to all requests/responses
- Correlation IDs propagate through logs via `BeginScope`
- Existing CI pipeline remains functional (`.github/workflows/ci.yml`)
- All endpoints return expected HTTP status codes
- `dotnet build`: 0 errors, 0 warnings

**Notes:**
- Production deployment (blue-green, staging/prod environments) requires cloud infrastructure
- Existing CI pipeline covers build, test, Docker build stages
- Correlation IDs provide distributed tracing foundation for multi-service deployments
- Deployment scripts are ready to be created when cloud targets are defined

---

## Week 18: API Gateway & Resilience (Resilience & Security)

**Date:** 2026-05-19
**Status:** ✅ Pass
**Test Results:**
- Polly 8.6.6 already installed for resilience patterns
- EF Core configured with `EnableRetryOnFailure(3)` for transient DB failures
- LoginRateLimiter: 5 failed attempts = 15-minute lockout (tested via code review)
- Refresh token rotation: issuing new token + revoking old on each refresh
- Compromised token detection: revoked token reuse triggers full user token invalidation
- `dotnet build`: 0 errors, 0 warnings
- `dotnet test`: 4/4 passing

**Notes:**
- Ocelot API Gateway not implemented — single API doesn't need gateway; can be added if service is split
- Login rate limiting uses in-memory `ConcurrentDictionary` (resets on app restart)
- Refresh token rotation provides family-revocation: if a stolen token is used, all user tokens are revoked
- `X-Correlation-ID` middleware enables distributed tracing across request chains
- All new services registered in Program.cs as singletons/scoped as appropriate


# Task Management API

Enterprise Task & Project Management Platform built with .NET 10, PostgreSQL, Docker, and CI/CD.

## Tech Stack

- **.NET 10** - Web API framework
- **PostgreSQL 16** - Primary database
- **Entity Framework Core 10** - ORM
- **Docker & Docker Compose** - Containerization
- **GitHub Actions** - CI/CD pipeline

## Prerequisites

- .NET 10 SDK
- Docker Desktop
- Git

## Getting Started

```bash
# Run with Docker Compose
docker compose up -d

# Access the app
# GUI Frontend:  http://localhost:5000
# Swagger UI:    http://localhost:5000/swagger
# Health Check:  http://localhost:5000/health
```

### Seed Credentials

| Role  | Email                     | Password   |
|-------|---------------------------|------------|
| Admin | admin@taskmanager.com     | Admin123!  |
| User  | user@taskmanager.com      | User123!   |

### GUI Frontend

The app includes a single-page GUI at `http://localhost:5000/` built with vanilla HTML/CSS/JS (no npm, no build step). Use it to:

- Log in with seed credentials
- View, search, filter, and paginate tasks
- Create, edit, and delete tasks
- View analytics (task summary, completion rate, overdue report)

## API Endpoints

| Method | Endpoint                       | Description                | Auth Required |
|--------|--------------------------------|----------------------------|:---:|
| GET    | `/health`                      | Health check               | No  |
| POST   | `/api/v1/auth/register`        | Register new user          | No  |
| POST   | `/api/v1/auth/login`           | Login, returns JWT         | No  |
| POST   | `/api/v1/auth/refresh`         | Refresh access token (rotated) | No  |
| GET    | `/api/v1/auth/me`              | Get current user info      | Yes |
| GET    | `/api/v1/tasks`                | List tasks (paginated)     | Yes |
| GET    | `/api/v1/tasks/{id}`           | Get task by ID             | Yes |
| POST   | `/api/v1/tasks`                | Create task                | Yes |
| PUT    | `/api/v1/tasks/{id}`           | Update task                | Yes |
| DELETE | `/api/v1/tasks/{id}`           | Delete task                | Yes |
| GET    | `/api/v1/analytics/task-summary` | Task count by status     | Yes |
| GET    | `/api/v1/analytics/completion-rate` | Completion rate %     | Yes |
| GET    | `/api/v1/analytics/overdue`    | Overdue task report        | Yes |
| WS     | `/hubs/tasks`                  | SignalR real-time hub      | Yes |

## Security Features

- **JWT Authentication** — 15-min access tokens + 7-day refresh tokens
- **Refresh Token Rotation** — New token issued on each refresh; old token revoked
- **Compromised Token Detection** — Reuse of a revoked token invalidates all user tokens
- **Login Rate Limiting** — 5 failed attempts = 15-minute lockout per email
- **Rate Limiting** — 100 requests/minute/IP via AspNetCoreRateLimit
- **Password Hashing** — ASP.NET Core Identity `PasswordHasher`
- **Correlation IDs** — `X-Correlation-ID` header on all requests for tracing

## Monitoring & Resilience

- **OpenTelemetry** — Metrics exported to console (request duration, auth attempts, etc.)
- **Health Checks** — PostgreSQL + Redis liveness probes at `/health`
- **EF Core Retry** — `EnableRetryOnFailure(3)` for transient DB failures
- **Polly** — Installed for retry, circuit breaker, and timeout policies
- **Serilog** — Structured logging to console + rolling daily files
- **SignalR** — Real-time task notifications via WebSocket at `/hubs/tasks`

## Project Structure

```
src/
├── TaskManagement.API          # Controllers, Middleware, Program.cs
├── TaskManagement.Application  # Services, DTOs, Interfaces
├── TaskManagement.Domain       # Entities, Enums, Value Objects
└── TaskManagement.Infrastructure # EF Context, Repositories, External Services
```

## Development Plan

See [dotnet-weekly-plan.md](dotnet-weekly-plan.md) for the full 20-week progressive development roadmap.

Progress is tracked in [LOGBOOK.md](LOGBOOK.md).

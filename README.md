# CloudOps AI Platform

A portfolio-quality, full-stack delivery-management platform built for .NET and cloud engineering roles. The current release deliberately focuses on the secure project/task-management foundation; AI-assisted features are a later extension.

## Architecture

The backend follows a dependency-inward layered architecture:

```text
CloudOps.Api             HTTP, JWT pipeline, controllers, Swagger
        ↓
CloudOps.Application     DTOs, use cases, repository abstractions, authorization rules
        ↓
CloudOps.Domain          Project/task entities and business state
        ↑
CloudOps.Infrastructure  EF Core/PostgreSQL, ASP.NET Identity, JWT token creation
```

The API only returns Application DTOs—never database entities. Infrastructure implements the Application repository interfaces, while ASP.NET Core's dependency injection container wires everything together. The React app is a separate TypeScript/Vite client with an API service layer and protected routes.

## Features

- Registration and login using ASP.NET Core Identity and JWT bearer tokens.
- Four seeded roles: `Admin`, `ProjectManager`, `Developer`, and `Viewer`. New registrations begin as `Developer` so the self-service workspace is immediately usable; role administration can be added as an admin-only feature.
- Project CRUD, task creation/update/delete, task status and priority tracking.
- Role-aware task write endpoints, project ownership checks, structured problem-details errors.
- PostgreSQL persistence through EF Core, Swagger UI, xUnit business-logic tests.
- Responsive React dashboard, project portfolio and Kanban-style task board.
- Docker Compose services for PostgreSQL, API, and web client.

## Prerequisites

- .NET SDK 8.0+
- Node.js 20+ and npm
- PostgreSQL 16+ (or Docker)

## Local setup

Copy the environment template, choose real local-only values, and do not commit the resulting `.env` file:

```bash
cp .env.example .env
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=cloudops;Username=cloudops;Password=your-password'
export Jwt__Issuer='CloudOps.Api'
export Jwt__Audience='CloudOps.Web'
export Jwt__Secret='a-random-development-secret-with-at-least-32-bytes'
export CLOUDOPS_DEFAULT_ADMIN_EMAIL='admin@cloudops.local'
export CLOUDOPS_DEFAULT_ADMIN_PASSWORD='AdminPass123!'
```

For local development, the app seeds a default admin user when those `CLOUDOPS_DEFAULT_ADMIN_*` variables are present. The default admin login is `admin@cloudops.local` with password `AdminPass123!`.

To enable the delivery assistant, configure the provider through environment variables only. For Azure OpenAI, set `Ai__ApiKey`, `Ai__Endpoint`, and `Ai__Deployment`; for an OpenAI-compatible endpoint, set `Ai__ApiKey`, `Ai__Endpoint`, and optionally `Ai__Model`. Never commit the API key or a populated `.env` file.

Create the initial EF Core migration once, then apply it:

```bash
dotnet tool restore
dotnet ef migrations add InitialCreate --project src/CloudOps.Infrastructure --startup-project src/CloudOps.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/CloudOps.Infrastructure --startup-project src/CloudOps.Api
```

In the `Development` environment only, the API also uses `EnsureCreated` to make a brand-new local database immediately usable. Generate and commit the migration above before deploying or sharing a database; use migrations—not `EnsureCreated`—for real environments.

Run the API and client in separate terminals:

```bash
dotnet run --project src/CloudOps.Api
cd frontend && cp .env.example .env && npm install && npm run dev
```

The API is served on the URL printed by ASP.NET Core (Swagger at `/swagger`); Vite runs at `http://localhost:5173` by default. Set `VITE_API_URL` in `frontend/.env` to the API base path, for example `http://localhost:5080/api`.

## Docker

Create `.env` from the template and provide a strong JWT secret, then build the services:

```bash
docker compose up --build
```

Before first use, generate and commit the EF migration using the local setup command above. Docker Compose uses `http://localhost:8080` for the API and `http://localhost:5173` for the web app.

## Tests and verification

```bash
dotnet build CloudOps.sln
dotnet test CloudOps.sln
cd frontend && npm run build
```

## API overview

| Area | Endpoints |
| --- | --- |
| Authentication | `POST /api/auth/register`, `POST /api/auth/login` |
| Dashboard | `GET /api/dashboard` |
| Projects | `GET/POST /api/projects`, `GET/PUT/DELETE /api/projects/{id}` |
| Tasks | `GET/POST /api/projects/{projectId}/tasks`, `GET/PUT/DELETE /api/tasks/{id}` |

Use the `Authorization: Bearer <token>` header for protected endpoints. Swagger provides an interactive API surface; bearer security metadata can be added alongside a future refresh-token flow.

## Security notes

Configuration is intentionally environment driven. Neither database credentials nor JWT keys appear in source code. For deployment, use a managed secret store (such as Azure Key Vault), managed PostgreSQL, HTTPS termination, and a rotation policy for signing keys.

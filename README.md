# CloudOps AI Platform

CloudOps is a portfolio-ready delivery-management platform for teams. It combines secure project and task management with a provider-agnostic AI delivery assistant.

## Architecture

```text
React + TypeScript ──► Vercel ──► ASP.NET Core API ──► Render ──► Supabase PostgreSQL
                                      │
                                      └── Swagger /health
```

The backend follows a layered architecture: `CloudOps.Api` (HTTP), `CloudOps.Application` (use cases and DTOs), `CloudOps.Domain` (business model), and `CloudOps.Infrastructure` (EF Core, Identity, PostgreSQL, and external providers).

## Technology

- React, TypeScript, Vite, Axios
- ASP.NET Core 8, ASP.NET Identity, JWT bearer authentication
- Entity Framework Core 8 and PostgreSQL (Supabase in production)
- Docker, Render, Vercel, GitHub Actions, xUnit

## Features and authorization

Users can register, authenticate, manage authorized projects and tasks, and use the AI delivery assistant. Roles are `Admin`, `ProjectManager`, `Developer`, and `Viewer`. The API—not only the UI—enforces role and project-resource authorization. Admins manage all protected resources; project managers manage authorized projects; developers can update permitted tasks; viewers have read-only access.

## Local development

Prerequisites: .NET SDK 8+, Node.js 20+, and PostgreSQL 16+ (or Docker).

```bash
cp .env.example .env
cp frontend/.env.example frontend/.env
dotnet ef database update --project src/CloudOps.Infrastructure --startup-project src/CloudOps.Api
dotnet run --project src/CloudOps.Api
```

In another terminal:

```bash
cd frontend
npm ci
npm run dev
```

Set local values through environment variables or your uncommitted `.env` file. At minimum, configure `ConnectionStrings__DefaultConnection`, `Jwt__Issuer`, `Jwt__Audience`, and a `Jwt__Secret` of at least 32 bytes. `frontend/.env` supplies `VITE_API_URL` (normally `http://localhost:5080/api`). The API exposes Swagger at `/swagger` and health status at `/health`.

## Database and migrations

EF Core migrations are committed in `src/CloudOps.Infrastructure/Persistence/Migrations`. Apply them locally or against a configured database with:

```bash
dotnet ef database update --project src/CloudOps.Infrastructure --startup-project src/CloudOps.Api
```

Production accepts either `DATABASE_URL` (including Supabase's `postgresql://...` URI) or `ConnectionStrings__DefaultConnection`. Never commit either value. See [DEPLOYMENT.md](DEPLOYMENT.md) for Supabase and Render migration steps.

## Docker

The API image uses an SDK build stage and a small ASP.NET runtime stage. It binds to the platform-provided `PORT` (or 10000 locally):

```bash
docker build -f Dockerfile.api -t cloudops-api .
docker compose up --build
```

The Compose frontend is built with `VITE_API_URL=http://localhost:8080/api`; use `http://localhost:5173` in the browser. Docker build context ignores `.env` files and local build output.

## Tests and CI

```bash
dotnet build CloudOps.sln
dotnet test CloudOps.sln
cd frontend && VITE_API_URL=https://api.example.invalid/api npm run build
```

GitHub Actions runs the same restore, build, backend-test, and frontend-build checks on every push and pull request. It uses no secrets.

## Deployment

The intended production path is **React → Vercel → ASP.NET Core API → Render → Supabase PostgreSQL**. Configure Vercel's `VITE_API_URL` as `https://<render-service>.onrender.com/api`; configure Render with `DATABASE_URL`, JWT settings, and the exact `CORS_ALLOWED_ORIGINS` Vercel URL. The frontend and API URLs are intentionally not committed because they are created per deployment.

Follow the beginner-friendly, reproducible guide in [DEPLOYMENT.md](DEPLOYMENT.md). Once deployed, record your public URLs here:

| Service | Public URL |
| --- | --- |
| Frontend (Vercel) | `https://<your-vercel-project>.vercel.app` |
| API (Render) | `https://<your-render-service>.onrender.com` |
| API documentation | `https://<your-render-service>.onrender.com/swagger` |

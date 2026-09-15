# Deploying CloudOps: Vercel, Render, and Supabase

This guide deploys CloudOps without committing secrets. It assumes the repository is pushed to GitHub and the checked-in EF Core migrations represent the model you want to run.

## 1. Prepare GitHub

Push the repository to GitHub. Do not add `.env`, `frontend/.env`, database URIs, JWT secrets, or provider API keys. The included `.gitignore`, `.dockerignore`, and CI workflow are designed to keep these out of source control. Confirm the GitHub Actions **CI** workflow is green before connecting hosting services.

## 2. Create Supabase PostgreSQL

1. Create a new Supabase project and wait for its database to become available.
2. In **Connect**, copy the PostgreSQL **URI** or connection string. For long-lived server connections, use the pooler URI recommended by Supabase when available.
3. Keep the copied value private. It becomes Render's `DATABASE_URL`; do not paste it into a committed file.

CloudOps accepts both normal Npgsql key/value strings and `postgres://` or `postgresql://` URIs. URI values are converted to a secure Npgsql connection configuration, including required TLS.

## 3. Apply database migrations

Run this from a trusted machine with the .NET 8 SDK, after temporarily setting `DATABASE_URL` to the Supabase value:

```bash
export DATABASE_URL='postgresql://...'
dotnet ef database update --project src/CloudOps.Infrastructure --startup-project src/CloudOps.Api
```

Alternatively, for the first Render deploy only, set `Database__MigrateOnStartup=true`. The API applies its existing migrations at startup. After a successful deploy, set it back to `false`; future schema changes should be reviewed and migrated deliberately with the command above. Do not use `EnsureCreated` for production.

In Supabase **Table Editor** or SQL Editor, verify that Identity tables (such as `AspNetUsers` and `AspNetRoles`) and CloudOps tables were created. The API seeds roles at startup. A bootstrap administrator is created only if you deliberately provide the optional `CLOUDOPS_DEFAULT_ADMIN_EMAIL` and `CLOUDOPS_DEFAULT_ADMIN_PASSWORD` variables.

## 4. Deploy the API on Render

1. In Render, choose **New → Web Service**, connect the GitHub repository and select the deployment branch.
2. Choose **Docker** and set the Dockerfile path to `Dockerfile.api`. The production image is multi-stage and needs no local SDK or PostgreSQL installation.
3. Select a free instance if it is available for your Render account. Free instances can cold-start, so allow time for the first request.
4. Set the health-check path to `/health`.
5. Add these environment variables in Render's dashboard:

| Variable | Value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `DATABASE_URL` | Supabase PostgreSQL URI (secret) |
| `Jwt__Issuer` | A stable issuer name, e.g. `CloudOps.Api` |
| `Jwt__Audience` | A stable audience name, e.g. `CloudOps.Web` |
| `Jwt__Secret` | Newly generated secret of 32+ bytes (secret) |
| `CORS_ALLOWED_ORIGINS` | Exact Vercel origin, e.g. `https://your-project.vercel.app` |
| `Database__MigrateOnStartup` | `true` only for the controlled initial migration, otherwise `false` |

`PORT` is supplied by Render; do not set it. The Docker entrypoint listens on it automatically. Add optional AI provider and bootstrap-admin variables only when needed, and treat them as secrets. Render should report a successful `/health` check before the frontend is deployed.

The API intentionally refuses to start in Production without `CORS_ALLOWED_ORIGINS` (or the legacy `FrontendUrl`) rather than silently allowing every origin. Use a comma-separated list only for explicit additional origins, such as a separately approved preview deployment.

## 5. Deploy the frontend on Vercel

1. In Vercel, import the same GitHub repository.
2. Set **Root Directory** to `frontend`. Vercel detects Vite; the build command is `npm run build` and output directory is `dist`.
3. In Vercel project environment variables, add `VITE_API_URL` with `https://<render-service>.onrender.com/api`. Do not add a trailing slash.
4. Deploy. The build deliberately fails if a production `VITE_API_URL` is absent, preventing an accidental localhost production build.
5. Copy the resulting production Vercel URL into Render's `CORS_ALLOWED_ORIGINS`, redeploy Render, then redeploy Vercel if the API URL changed.

Vite exposes `VITE_*` values to the browser, so `VITE_API_URL` may contain the public API URL but must never contain a secret.

## 6. Validate production

Use the public URLs, not localhost:

1. Open `https://<render-service>.onrender.com/health`; it should return `{"status":"healthy"}`.
2. Open `/swagger` and confirm the API documents its endpoints.
3. Register or log in, then confirm the browser can load projects and tasks without a CORS error.
4. Test project and task CRUD, search/filtering, validation errors, and refresh a nested frontend route.
5. Check authorization with each role: Admin and authorized project managers may manage permitted resources; developers may only make permitted task updates; viewers remain read-only. Also send a request without a token and verify it is rejected.
6. Confirm Supabase receives the writes and Render logs contain no recurring 500 errors.

The backend is the authorization boundary; hidden frontend buttons are only a usability feature.

## Troubleshooting

| Symptom | Likely fix |
| --- | --- |
| Render health check fails | Check `DATABASE_URL`, Supabase network/TLS settings, migration status, and Render logs. |
| API fails at startup with CORS configuration error | Set `CORS_ALLOWED_ORIGINS` to the exact `https://...vercel.app` origin, without a path. |
| Browser reports CORS error | The Vercel origin and Render CORS value must match exactly; redeploy Render after changing it. |
| Vercel build fails | Set `VITE_API_URL` for the Production environment and ensure it ends in `/api`. |
| API starts but tables are missing | Run `dotnet ef database update` with `DATABASE_URL`, or temporarily enable `Database__MigrateOnStartup`. |
| API returns 401/403 | Obtain a fresh login token and verify the account's role and project membership. |
| Render service is slow after inactivity | This is expected on free instances; wait for the cold start and retry. |

When moving to a different production domain, update `CORS_ALLOWED_ORIGINS`, Vercel's `VITE_API_URL` if required, and redeploy both services. Rotate JWT, database, and AI-provider secrets through the host dashboards rather than changing source code.

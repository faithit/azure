# Render — API Setup (CloudOps)

Use this file as a checklist and copy-paste reference when configuring the Render Web Service for the CloudOps API.

Important: Do NOT paste secrets into source control. Use Render's secret environment variables UI.

## Web Service settings
- Service type: **Web Service** (Docker)
- Dockerfile path: `Dockerfile.api`
- Health check path: `/health`
- Auto-deploy: enabled (optional)

## Environment variables (add via Render dashboard -> Environment -> Add Environment Variable)

| Name | Example value / notes | Secret? |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` | No |
| `DATABASE_URL` | `postgresql://<user>:<pass>@dbhost:5432/<db>?sslmode=require` | Yes |
| `Jwt__Issuer` | `CloudOps.Api` | No |
| `Jwt__Audience` | `CloudOps.Web` | No |
| `Jwt__Secret` | (random 32+ char string) | Yes |
| `CORS_ALLOWED_ORIGINS` | `https://your-vercel-project.vercel.app` (exact origin) | No |
| `Database__MigrateOnStartup` | `true` (only for initial deploy), then set to `false` | No |
| `CLOUDOPS_DEFAULT_ADMIN_EMAIL` | (optional) bootstrap admin | Yes |
| `CLOUDOPS_DEFAULT_ADMIN_PASSWORD` | (optional) bootstrap password | Yes |
| `PORT` | Leave unset — Render supplies this automatically | No |

Notes:
- Use the Supabase-provided connection string as `DATABASE_URL`. The app supports `postgres://` and `postgresql://` URIs and converts them to Npgsql format.
- Set `Database__MigrateOnStartup=true` only for the controlled, initial deploy so that migrations run once at startup. After a successful deployment, set it back to `false` and run future migrations manually from a trusted machine.
- `CORS_ALLOWED_ORIGINS` must exactly match your Vercel origin (no trailing slash). For multiple origins, use a comma-separated list.

## Deployment checklist
1. Create the Render Web Service and connect the GitHub repo+branch.
2. Set the Dockerfile to `Dockerfile.api` and health-check to `/health`.
3. Add the environment variables above (mark secrets).
4. Deploy and monitor logs. Expected startup behavior:
   - App starts, applies migrations (if `Database__MigrateOnStartup=true`), seeds roles, and reports healthy on `/health`.
5. After verifying migrations and data, set `Database__MigrateOnStartup=false`.

## Troubleshooting
- If `/health` returns 503, check Render logs for database connectivity and ensure `DATABASE_URL` is correct.
- If CORS errors occur, verify `CORS_ALLOWED_ORIGINS` matches Vercel exactly and redeploy Render.
- For migration errors, fetch the logs and run migrations locally against the same `DATABASE_URL` for additional debugging.


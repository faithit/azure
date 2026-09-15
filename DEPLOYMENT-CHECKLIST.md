# Deployment Checklist — CloudOps (Phase 6)

This concise checklist walks through deploying the app to Vercel (frontend), Render (API), and Supabase (Postgres). Follow each step and mark it done.

---

## Preparations (local / GitHub)

- [ ] Ensure repository is pushed to GitHub and CI is green (`.github/workflows/ci.yml`).
- [ ] Do NOT commit secrets; `.gitignore` already excludes `.env` and `.env.*`.
- [ ] Generate the following secrets locally (don't commit):
  - `DATABASE_URL` (Supabase)
  - `Jwt__Secret` (32+ bytes)
  - `Jwt__Issuer` and `Jwt__Audience`
  - Optional: `CLOUDOPS_DEFAULT_ADMIN_EMAIL` and `CLOUDOPS_DEFAULT_ADMIN_PASSWORD`

---

## 1) Create Supabase (PostgreSQL)

1. Create a new Supabase project and wait for provisioning.
2. In the Supabase dashboard → "Settings" → "Database" → "Connection string" copy the **postgres** (pooler if available) URI. This will look like:

   `postgresql://<user>:<password>@<host>:5432/<db>?sslmode=require`

3. Keep the value secret. You will use it as `DATABASE_URL` on Render.

---

## 2) Prepare and test migrations locally (optional but recommended)

On a secure machine with .NET 8 SDK and `DATABASE_URL` set, run:

```bash
export DATABASE_URL='postgresql://...'
dotnet ef database update --project src/CloudOps.Infrastructure --startup-project src/CloudOps.Api
```

Verify tables in Supabase Table Editor (e.g. `AspNetUsers`, `Projects`, `Tasks`).

---

## 3) Create Render Web Service (API)

1. On Render, click **New → Web Service**.
2. Connect your GitHub repository and select the branch to deploy.
3. Select **Docker** and set the Dockerfile path to `Dockerfile.api`.
4. Set the health check to `/health`.
5. Add environment variables (mark secrets):
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `DATABASE_URL` = (Supabase URI, secret)
   - `Jwt__Issuer` = `CloudOps.Api`
   - `Jwt__Audience` = `CloudOps.Web`
   - `Jwt__Secret` = (32+ char secret, secret)
   - `CORS_ALLOWED_ORIGINS` = `https://<your-vercel-project>.vercel.app`
   - `Database__MigrateOnStartup` = `true` (only for initial migration)

6. Deploy and wait. Check Render logs for startup & the `/health` endpoint.
7. Once migrations applied successfully, set `Database__MigrateOnStartup=false`.

Notes:
- Render supplies `PORT`; `Dockerfile.api` already uses `--urls http://+:${PORT:-10000}`.
- If the API fails with a CORS configuration error, ensure `CORS_ALLOWED_ORIGINS` matches your Vercel URL exactly (no trailing slash).

---

## 4) Deploy frontend to Vercel

1. In Vercel, import the repo and set the **Root Directory** to `frontend`.
2. Add Environment Variables (Production):
   - `VITE_API_URL` = `https://<your-render-service>.onrender.com/api` (no trailing slash)
3. Build & Deploy.
4. Copy the Vercel production URL and add it to Render's `CORS_ALLOWED_ORIGINS`, then redeploy Render if you changed it.

---

## 5) Post-deploy verification (smoke tests)

- [ ] Frontend loads at `https://<your-vercel>.vercel.app`.
- [ ] API `https://<your-render>.onrender.com/health` returns `{"status":"healthy"}`.
- [ ] API Swagger (if enabled) is available only when `Swagger:Enable=true` or in Development.
- [ ] Register/login from frontend and confirm JWT-authenticated requests succeed.
- [ ] Create/read/update/delete Projects and Tasks.
- [ ] Verify role authorization: Admin, ProjectManager, Developer, Viewer behaviors.
- [ ] Check that DB writes appear in Supabase.
- [ ] Refresh nested frontend routes and verify no 404s.

---

## 6) GitHub Actions & CI

- CI at `.github/workflows/ci.yml` runs build/tests and frontend build on PRs and pushes. Ensure secrets are added only to Vercel/Render, not to the repo.

---

## 7) Rollback plan

- If migration or deployment fails, restore previous image in Render (or redeploy prior commit) and set `Database__MigrateOnStartup=false`. Run migrations manually from a trusted machine to apply fixes.

---

## Useful commands

```bash
# Build & test locally
dotnet build CloudOps.sln
dotnet test CloudOps.sln
# Apply migrations
export DATABASE_URL='postgresql://...'
dotnet ef database update --project src/CloudOps.Infrastructure --startup-project src/CloudOps.Api
# Build Docker image locally
docker build -f Dockerfile.api -t cloudops-api .
# Build frontend locally (production)
VITE_API_URL=https://api.example.invalid/api npm run build --prefix frontend
```

---

## Security checklist

- [ ] No `.env` or secrets committed.
- [ ] Rotate `Jwt__Secret` if suspected leakage.
- [ ] Use Render/Vercel secret stores for runtime secrets.

---

## Completion

When all checks above are done and verified, mark this checklist complete and record:
- Frontend URL: https://<your-vercel>.vercel.app
- API URL: https://<your-render>.onrender.com
- Supabase project name/ID (private record)



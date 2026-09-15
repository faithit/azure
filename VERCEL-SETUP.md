# Vercel — Frontend Setup (CloudOps)

Steps to deploy the React + Vite frontend to Vercel and connect it to the Render API.

## Project import
1. In Vercel, choose **New Project → Import Git Repository** and select this repository.
2. Set **Root Directory** to `frontend`.
3. Vercel detects Vite — default build command should be `npm run build` and output directory `dist`.

## Environment variables (Production)
- `VITE_API_URL` = `https://<your-render-service>.onrender.com/api`
  - Must be the public API base URL that ends with `/api` (no trailing slash after `/api`).

## Preview & Development
- You can add a `Preview` environment variable for Vercel preview deployments if you use preview environments. Add the same `VITE_API_URL` pointing to a staging API or the Render preview service.

## Build options
- If the build fails because `VITE_API_URL` is missing, Vite config enforces having it for production builds. Add the environment variable in the Vercel UI under *Environment Variables*.

## Post-deploy
1. Copy the Vercel production URL (e.g. `https://<project>.vercel.app`).
2. Add that URL to Render's `CORS_ALLOWED_ORIGINS` and redeploy the Render service so the API accepts requests from the frontend origin.
3. Optionally, set up a custom domain in Vercel and update Render CORS accordingly.

## Notes
- `VITE_*` variables are exposed to client-side code — never put secrets in them.
- To test locally against Render, set `VITE_API_URL` in `frontend/.env` (uncommitted).


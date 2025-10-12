# Copilot instructions for Frnq

Purpose
- This file guides GitHub Copilot (Chat and the PR “work on tasks” coding agent) to follow our stack, conventions, quality gates, and security practices.
- Prefer small, focused changes; describe the impact and test plan in PRs. Ask clarifying questions in PR description when blocked.

Repo layout
- Backend: `src/backend/frnq-api/` (ASP.NET Core 9, EF Core 9, PostgreSQL, JWT, RazorLight, OpenAPI in Development)
- Frontend: `src/frontend/` (SvelteKit 2, Svelte 5, TypeScript, Tailwind v4, Vite, ESLint/Prettier, svelte-i18n)
- Dev UI origin: http://localhost:5173

General conventions
- Keep controllers/pages thin; move logic to services/management classes (backend) and `src/lib/services` (frontend).
- All I/O is async. On backend, accept and propagate `CancellationToken` to EF/database calls.
- Do not log or return secrets. Never expose password hashes.
- Prefer incremental, low-risk PRs. Include screenshots or JSON samples when helpful.

Backend (ASP.NET Core) guidelines
- Controllers
  - Use `[ApiController]` with attribute routing under `api/<feature>`.
  - Inject dependencies via constructor; prefer `ILogger<T>`.
  - Accept `CancellationToken ct` and propagate it.
  - Return `ActionResult<T>` for typed responses; include `[ProducesResponseType]` for 200/400/401/403/404 as appropriate.
- Structure
  - Entities/Models: `*Model.cs`.
  - DTOs/View DTOs: `*Dto.cs` / `*ViewDto.cs`.
  - Business logic: `*Management` classes in feature folders (e.g., `Investments`, `Positions`, `Auth`).
  - Auth: use `Authorize` attributes to guard endpoints; validate roles/ownership in management classes.
- EF Core (Npgsql/PostgreSQL)
  - Default reads with `.AsNoTracking()`.
  - Project directly to DTOs in queries to avoid over-fetching.
  - Use async methods only; avoid sync-over-async and implicit lazy-loading.
  - Keep queries paginated when returning collections (Skip/Take with sensible limits).
- CORS
  - Use `DevCorsPolicy` (http://localhost:5173) in development and `DefaultCorsPolicy` otherwise.
- Error handling
  - Map domain errors to ProblemDetails with proper status codes; avoid leaking internal exceptions.

Frontend (SvelteKit) guidelines
- Structure
  - Routes in `src/routes` with `+page.svelte`, `+page.ts`, and `+server.ts` as needed.
  - Shared UI in `src/lib/Components`; logic/services in `src/lib/services`; shared types in `src/lib/types`.
- Coding
  - Strong TypeScript typing; export clear interfaces for API data and forms.
  - Centralize HTTP in `src/lib/services` and add Authorization header when a JWT is present.
  - Prefer `load` for data fetching; use `onMount` only for client-only behaviors.
  - Use Tailwind CSS v4 utilities; avoid inline styles when possible.
- Accessibility
  - Label form controls and use `aria-*` for errors and descriptions.

Docker/infra guidelines
- Pin image versions; prefer slim variants; add healthchecks.
- Align CORS and environment with backend `Program.cs`.
- Postgres via official image; persist with volumes; never commit secrets.

Build, check, and run (reference)
- Backend
  - Build: `dotnet build src/backend/frnq-api/frnq-api.csproj`
  - Run (dev): `dotnet run --project src/backend/frnq-api/frnq-api.csproj`
  - EF migrations (if needed): `dotnet ef migrations add <Name> -s src/backend/frnq-api -p src/backend/frnq-api`
- Frontend
  - Install: `npm ci` in `src/frontend`
  - Dev: `npm run dev` (generates translation types first)
  - Build: `npm run build`
  - Type/lint: `npm run check` and `npm run lint`

Quality gates (must pass before PR complete)
- Backend: build succeeds without errors; no obvious runtime nullability issues; endpoints return correct status codes.
- Frontend: `npm run check` and `npm run lint` pass; no TypeScript errors or ESLint violations.
- Tests: if tests exist for touched areas, update/add and ensure they pass.
- Smoke: minimal manual verification for new endpoints/components (document in PR).

Security and privacy
- Do not add secrets to source control or sample configs; use environment variables.
- Sanitize logs. Do not return internal exception messages to clients.
- Respect auth/roles; do not widen CORS without explicit reason.

Branch/PR guidelines
- Branch names: `copilot/<issue-key>-<short-title>`.
- Always in present tense. Maximum 70 characters.
- PR description includes: context, approach, risk/rollout, test plan, screenshots (if UI).

Default patterns to generate
- Backend endpoint: thin controller method -> calls a `*Management` service -> maps to View DTO -> returns proper status code with `[ProducesResponseType]` and uses `CancellationToken`.
- EF Core list: `.AsNoTracking()`, filters, order, projection to DTO, pagination with `Skip/Take`.
- Frontend form: Svelte component with bound inputs, i18n labels/messages, accessible errors, centralized service call, loading/disabled states.
- API client: typed helper in `src/lib/services/*.ts` using fetch with proper headers and error mapping.

If unsure
- Ask in the PR description: desired route/DTOs, required roles (public vs admin), UX behavior, and data constraints.

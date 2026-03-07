# AGENTS.md

This file defines operating guidance for coding agents working in this repository.

## Project identity

- Canonical project/product reference is `BASKENT DOVIZ`.
- In agent messages, docs, and summaries, prefer `BASKENT DOVIZ` name.
- `SmileMedical` and `AnasıTAS_Deniz` labels are technical path names; do not use them as the primary project name in communication.

## Scope and priorities

- Apply these instructions for the whole repository unless a deeper `AGENTS.md` overrides them.
- Prioritize edits in active project paths:
  - `SmileMedical/SmileMedical.API`
  - `SmileMedical/SmileMedical.Business`
  - `SmileMedical/SmileMedical.Data`
  - `SmileMedical/SmileMedical.Entity`
  - `SmileMedical/frontend`
  - `AnasıTAS_Deniz.API`
  - `AnasıTAS_Deniz.Business`
  - `AnasıTAS_Deniz.Data`
  - `AnasıTAS_Deniz.Entity`
- Treat these as archive/import areas unless explicitly requested:
  - `SmileMedical/_nested_conflict_review`
  - `external/local_import`

## Repo quick map

- Backend: .NET 8 Web API + EF Core (SQL Server by default, SQLite possible in some setups).
- Frontend: Vue 3 + Vite (`SmileMedical/frontend`).
- Backend tests:
  - `SmileMedical/SmileMedical.API.Tests`
  - `AnasıTAS_Deniz.Tests`
- Operational docs and scripts:
  - `docs/`
  - `scripts/`
  - `cursor-agent/`

## Language and communication

- Use Turkish for Turkish prompts; use English for English prompts.
- Keep explanations concise and implementation-focused.
- Do not expose secrets or copy `.env` / `appsettings.json` secrets into code or logs.

## Coding guidelines

- Prefer small, targeted diffs over broad refactors.
- Preserve existing project conventions in each subproject (naming, folder structure, style).
- Avoid adding heavy new dependencies unless required by the task.
- Add brief comments only where logic is non-obvious.

## Setup and run commands

### .NET solutions

- Restore/build root solution:
  - `dotnet restore AnasıTAS_Deniz.sln`
  - `dotnet build AnasıTAS_Deniz.sln -c Debug`
- Restore/build BASKENT DOVIZ solution (folder name: `SmileMedical`):
  - `dotnet restore SmileMedical/SmileMedical.sln`
  - `dotnet build SmileMedical/SmileMedical.sln -c Debug`

### Run APIs

- BASKENT DOVIZ API (folder name: `SmileMedical`):
  - `dotnet run --project SmileMedical/SmileMedical.API/SmileMedical.API.csproj`
- AnasıTAS API:
  - `dotnet run --project AnasıTAS_Deniz.API/AnasıTAS_Deniz.API.csproj`

### Frontend

- `cd SmileMedical/frontend`
- `npm install`
- `npm run dev`
- Optional checks:
  - `npm run build`
  - `npm run test:run`
  - `npm run test:e2e`

## Testing expectations

- Always run the smallest relevant test scope for changed code:
  - Backend API change -> run the closest related `dotnet test` project first.
  - Frontend component/composable change -> run `npm run test:run` in `SmileMedical/frontend`.
  - Frontend route/UI behavior change -> run `npm run test:e2e` when feasible.
- If a change is documentation-only, tests may be skipped.
- When a test fails, include the failing command and error summary, then fix and rerun.

## Cursor Cloud specific instructions

- For non-trivial UI changes (`.vue`, `.tsx`, `.jsx`, `.css`, `.scss`, `.html`):
  - Perform manual GUI validation with computer-use tooling.
  - Record a short demo video that shows the happy path end-to-end.
  - Include at least one screenshot of final UI state.
- For backend or CLI-only changes:
  - Prefer terminal-driven verification (`dotnet test`, targeted script runs, API curl checks).
- Do not run full-repo test suites unless explicitly requested; run targeted checks first.
- Keep services running after validation unless cleanup is required to continue.

## Safety checks before finishing

- Confirm only intended files changed: `git status --short`.
- Review diff for accidental secret/config edits.
- If you added temporary debug code/logging for investigation, remove it before commit.

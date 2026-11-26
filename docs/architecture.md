# Architecture

```
Browser ──► Cloudflare ──► Worker (Starter.PWA: static app + security headers)
                │
                └── /api/* ──► Traefik ──► Starter.WebApi ──► PostgreSQL
```

One host serves both halves. Cloudflare runs the PWA's Worker for every path except `/api/*`, which goes past the
Worker to the server. There Traefik terminates HTTPS and hands `/api` to the API container. The app therefore calls
the API on its own origin, needs no CORS in production, and its Content-Security-Policy only allows `'self'`.

## Projects

| Project | Role | May reference |
| --- | --- | --- |
| `Starter.Persistence` | EF Core entities (`Entity/`). No logic. | nothing |
| `Starter.Abstractions` | Interfaces for logic and services (one per file), request/response models (`Models/`), domain exceptions. The contract between the API and the logic. | Persistence |
| `Starter` | Business logic (`Logic/`), `StarterContext`, migrations, DI registration (`AddStarterLogic`). | Abstractions |
| `Starter.WebApi` | Controllers, start-up (`Program.cs`), exception handling, health, Swagger. | Starter |
| `Starter.WebApi.UnitTests` | xUnit: logic on the EF in-memory provider, and the whole API hosted in memory. | WebApi |
| `Starter.WebApi.PostgresTests` | xUnit on a real PostgreSQL (Testcontainers): migrations, constraints. Not in the solution. | Starter |
| `Starter.PWA` | Vue 3 + Vuetify app and its Cloudflare Worker. | the API, through `src/logic/api.ts` |

## How a request flows

1. A component loads data with `useLoader(() => useApi().items.itemsList())`. The generated client
   (`src/logic/api.ts`) builds the request; `apiFetch` adds a timeout, retries idempotent reads on 502/503/504,
   records the API version and turns every failure into an `ApiError`.
2. The controller (`ItemsController`) binds and validates the request (`[ApiController]` returns 400 for invalid
   models) and calls the logic interface. It contains no rules.
3. The logic (`ItemLogic`) applies the rules, reads and writes through `StarterContext`, and returns models. When
   something is wrong it throws a domain exception.
4. `DomainExceptionHandler` turns that exception into a problem response (400, 403, 404 or 409) with
   `application/problem+json`. Anything else is a 500 without details.
5. Every response carries `X-App-Version`. The PWA shows a banner while it runs against a different release, which
   happens for a few seconds during a deploy.

## Adding a feature

1. Entity in `Starter.Persistence/Entity`, `DbSet` and mapping in `StarterContext`, then
   `dotnet ef migrations add <Name> --project Starter --startup-project Starter.WebApi`.
2. Models and the logic interface in `Starter.Abstractions`; the implementation in `Starter/Logic`, registered in
   `LogicServiceCollectionExtensions`.
3. A thin controller in `Starter.WebApi/Controllers`.
4. Tests: logic in `Starter.WebApi.UnitTests/Logic`, HTTP behaviour in `Starter.WebApi.UnitTests/Api`, database
   rules in `Starter.WebApi.PostgresTests`.
5. Run the API, regenerate the client (`pnpm --dir Starter.PWA run generate-api-schema`), build the view, and add
   its texts to `en.ts` and `nl.ts`.
6. Run the full suite in `AGENTS.md`.

The `Item` example shows each step; delete it once the project has its own first feature.

## What the template leaves out

A production app usually also needs authentication (JWT with refresh tokens), multi-tenancy, a separate
admin app, background workers, email, rate limiting and an audit log. The template leaves them out because every project needs
them differently; add them as the project needs them, following the same layering.

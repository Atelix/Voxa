# Architecture

```
Browser ──► Cloudflare ──► Worker (Voxa.PWA: static app + security headers)
                │
                └── /api/* ──► Traefik ──► Voxa.WebApi ──► PostgreSQL
```

One host serves both halves. Cloudflare runs the PWA's Worker for every path except `/api/*`, which goes past the
Worker to the server. There Traefik terminates HTTPS and hands `/api` to the API container. The app therefore calls
the API on its own origin, needs no CORS in production, and its Content-Security-Policy only allows `'self'`.

## Projects

| Project | Role | May reference |
| --- | --- | --- |
| `Voxa.Persistence` | EF Core entities (`Entity/`). No logic. | nothing |
| `Voxa.Abstractions` | Interfaces for logic and services (one per file), request/response models (`Models/`), domain exceptions. The contract between the API and the logic. | Persistence |
| `Voxa` | Business logic (`Logic/`), `VoxaContext`, migrations, DI registration (`AddVoxaLogic`). | Abstractions |
| `Voxa.WebApi` | Controllers, start-up (`Program.cs`), exception handling, health, Swagger. | Voxa |
| `Voxa.WebApi.UnitTests` | xUnit: logic on the EF in-memory provider, and the whole API hosted in memory. | WebApi |
| `Voxa.WebApi.PostgresTests` | xUnit on a real PostgreSQL (Testcontainers): migrations, constraints. Not in the solution. | Voxa |
| `Voxa.PWA` | Vue 3 + Vuetify app and its Cloudflare Worker. | the API, through `src/logic/api.ts` |

## How a request flows

1. A component loads data with `useLoader(() => useApi().items.itemsList())`. The generated client
   (`src/logic/api.ts`) builds the request; `apiFetch` adds a timeout, retries idempotent reads on 502/503/504,
   records the API version and turns every failure into an `ApiError`.
2. The controller (`ItemsController`) binds and validates the request (`[ApiController]` returns 400 for invalid
   models) and calls the logic interface. It contains no rules.
3. The logic (`ItemLogic`) applies the rules, reads and writes through `VoxaContext`, and returns models. When
   something is wrong it throws a domain exception.
4. `DomainExceptionHandler` turns that exception into a problem response (400, 403, 404 or 409) with
   `application/problem+json`. Anything else is a 500 without details.
5. Every response carries `X-App-Version`. The PWA shows a banner while it runs against a different release, which
   happens for a few seconds during a deploy.

## Adding a feature

1. Entity in `Voxa.Persistence/Entity`, `DbSet` and mapping in `VoxaContext`, then
   `dotnet ef migrations add <Name> --project Voxa --startup-project Voxa.WebApi`.
2. Models and the logic interface in `Voxa.Abstractions`; the implementation in `Voxa/Logic`, registered in
   `LogicServiceCollectionExtensions`.
3. A thin controller in `Voxa.WebApi/Controllers`.
4. Tests: logic in `Voxa.WebApi.UnitTests/Logic`, HTTP behaviour in `Voxa.WebApi.UnitTests/Api`, database
   rules in `Voxa.WebApi.PostgresTests`.
5. Run the API, regenerate the client (`pnpm --dir Voxa.PWA run generate-api-schema`), build the view, and add
   its texts to `en.ts` and `nl.ts`.
6. Run the full suite in `AGENTS.md`.

The `Item` example shows each step; delete it once the project has its own first feature.

## What the template leaves out

A production app usually also needs authentication (JWT with refresh tokens), multi-tenancy, a separate
admin app, background workers, email, rate limiting and an audit log. The template leaves them out because every project needs
them differently; add them as the project needs them, following the same layering.

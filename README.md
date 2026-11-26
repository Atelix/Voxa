# Voxa

Secret digital voting for a club's general meeting: members vote in advance or live in the hall with a personal code,
the board opens and closes items and adds paper votes, and the hall screen shows the results. One installation per
club; branding, mail sender and statute rules are settings. Read [docs/voxa.md](docs/voxa.md) for the yearly
workflow, the server settings, trial mode and how secrecy is guaranteed.

Voxa is built on the Atelix template: an ASP.NET Core API on PostgreSQL, a Vue 3 + Vuetify PWA on Cloudflare
Workers, CI/CD on GitHub Actions, and shared instructions and skills for Claude Code and Codex.

## What you get

| Part | What is in it |
| --- | --- |
| `Voxa.WebApi`, `Voxa`, `Voxa.Abstractions`, `Voxa.Persistence` | .NET 10 API in layers (controllers → logic → EF Core), problem-details errors, health and version endpoints, Swagger, migrations on start-up, Dockerfile. Analyzers at `latest-All` with warnings as errors. |
| `Voxa.WebApi.UnitTests`, `Voxa.WebApi.PostgresTests` | xUnit tests for logic and HTTP behaviour, plus real-PostgreSQL tests with Testcontainers. |
| `Voxa.PWA` | Vue 3, Vuetify 4, Pinia, vue-router, vue-i18n (en/nl), generated API client, loading/error helpers, a Worker with CSP and cache headers, light and dark theme. |
| `.github/` | CI with one required `CI result` check and path filters, PR previews of the Worker, release drafts, image publishing to ghcr.io, beta deploys on request, nightly production deploys, weekly SonarQube issues, Dependabot, CODEOWNERS, a branch ruleset, PR and issue templates. |
| `AGENTS.md`, `CLAUDE.md`, `.claude/`, `.agents/` | Repository rules and the definition of done, 16 skills (planning, TDD, debugging, security review, .NET, Vue, EF Core, Material Design, browser testing, ...), Google's Modern Web Guidance plugin, and a session-start hook that installs the toolchain in Claude Code on the web. |
| `docker-compose*.yml` | PostgreSQL for development; Traefik, the API, PostgreSQL and Watchtower for servers. |

## Run it locally

Requirements: .NET 10 SDK, Node (version in `.node-version`) with pnpm (`corepack enable`), and Docker for PostgreSQL.

```bash
# 1. Database
cp .env.development.example .env
docker compose up -d db

# 2. API on http://localhost:5143 (Swagger at /swagger)
dotnet tool restore
dotnet ef database update --project Voxa --startup-project Voxa.WebApi
dotnet run --project Voxa.WebApi

# 3. App on http://localhost:3000 (board at /bestuur, password "bestuur" in Development)
pnpm --dir Voxa.PWA install
pnpm --dir Voxa.PWA dev
```

## Checks

A change is done when the suite in [AGENTS.md](AGENTS.md#definition-of-valid-code) passes:

```bash
dotnet build Voxa.sln && dotnet test Voxa.sln
dotnet format whitespace Voxa.sln --verify-no-changes --exclude Voxa/Migrations
dotnet format style Voxa.sln --verify-no-changes --exclude Voxa/Migrations
pnpm --dir Voxa.PWA run type-check && pnpm --dir Voxa.PWA run lint
pnpm --dir Voxa.PWA run test && pnpm --dir Voxa.PWA run build
dotnet test Voxa.WebApi.PostgresTests   # database changes; needs Docker
```

After changing a controller or an API model, regenerate the client with
`pnpm --dir Voxa.PWA run generate-api-schema` while the API runs.

## Documentation

- [docs/architecture.md](docs/architecture.md): the projects, how a request flows, adding a feature
- [docs/languages.md](docs/languages.md): how the language is chosen, and adding a language
- [docs/continuous-integration.md](docs/continuous-integration.md): CI jobs, `CI result`, repository settings
- [docs/deployment.md](docs/deployment.md): beta and production, secrets, server and Cloudflare setup, rollbacks
- [docs/security-configuration.md](docs/security-configuration.md): settings, secrets and what is in place
- [docs/ai-development.md](docs/ai-development.md): the agent instructions and skills
- [docs/sonarqube.md](docs/sonarqube.md): optional weekly SonarQube Cloud analysis

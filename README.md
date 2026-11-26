# Atelix template

A starting point for a full-stack app, taken from the setup that runs Keeva in production: an ASP.NET Core API on
PostgreSQL, a Vue 3 + Vuetify PWA on Cloudflare Workers, CI/CD on GitHub Actions, and shared instructions and skills
for Claude Code and Codex.

## What you get

| Part | What is in it |
| --- | --- |
| `Starter.WebApi`, `Starter`, `Starter.Abstractions`, `Starter.Persistence` | .NET 10 API in layers (controllers → logic → EF Core), problem-details errors, health and version endpoints, Swagger, migrations on start-up, Dockerfile. Analyzers at `latest-All` with warnings as errors. |
| `Starter.WebApi.UnitTests`, `Starter.WebApi.PostgresTests` | xUnit tests for logic and HTTP behaviour, plus real-PostgreSQL tests with Testcontainers. |
| `Starter.PWA` | Vue 3, Vuetify 4, Pinia, vue-router, vue-i18n (en/nl), generated API client, loading/error helpers, a Worker with CSP and cache headers, light and dark theme. |
| `.github/` | CI with one required `CI result` check and path filters, PR previews of the Worker, release drafts, image publishing to ghcr.io, beta deploys on request, nightly production deploys, weekly SonarQube issues, Dependabot, CODEOWNERS, a branch ruleset, PR and issue templates. |
| `AGENTS.md`, `CLAUDE.md`, `.claude/`, `.agents/` | Repository rules and the definition of done, 16 skills (planning, TDD, debugging, security review, .NET, Vue, EF Core, Material Design, browser testing, ...), Google's Modern Web Guidance plugin, and a session-start hook that installs the toolchain in Claude Code on the web. |
| `docker-compose*.yml` | PostgreSQL for development; Traefik, the API, PostgreSQL and Watchtower for servers. |

The `Item` example (entity, logic, controller, tests, view) shows every layer once. Replace it with your first
feature.

## Start a new project

1. On GitHub, **Use this template → Create a new repository**.
2. Clone it and rename the template to your project:
   ```bash
   node tools/init/rename.mjs --name MyProject --owner @your-github-user
   ```
   Review `git diff`, then commit.
3. Set up the repository: import `.github/rulesets/protect-main.json` under **Settings → Rules → Rulesets**, enable
   auto-merge, and add the secrets and variables in [docs/deployment.md](docs/deployment.md) when you are ready to
   deploy. CI works without any of them.

## Run it locally

Requirements: .NET 10 SDK, Node (version in `.node-version`) with pnpm (`corepack enable`), and Docker for PostgreSQL.

```bash
# 1. Database
cp .env.development.example .env
docker compose up -d db

# 2. API on http://localhost:5143 (Swagger at /swagger)
dotnet tool restore
dotnet ef database update --project Starter --startup-project Starter.WebApi
dotnet run --project Starter.WebApi

# 3. App on http://localhost:3000
pnpm --dir Starter.PWA install
pnpm --dir Starter.PWA dev
```

## Checks

A change is done when the suite in [AGENTS.md](AGENTS.md#definition-of-valid-code) passes:

```bash
dotnet build Starter.sln && dotnet test Starter.sln
dotnet format whitespace Starter.sln --verify-no-changes --exclude Starter/Migrations
dotnet format style Starter.sln --verify-no-changes --exclude Starter/Migrations
pnpm --dir Starter.PWA run type-check && pnpm --dir Starter.PWA run lint
pnpm --dir Starter.PWA run test && pnpm --dir Starter.PWA run build
dotnet test Starter.WebApi.PostgresTests   # database changes; needs Docker
```

After changing a controller or an API model, regenerate the client with
`pnpm --dir Starter.PWA run generate-api-schema` while the API runs.

## Documentation

- [docs/architecture.md](docs/architecture.md): the projects, how a request flows, adding a feature
- [docs/continuous-integration.md](docs/continuous-integration.md): CI jobs, `CI result`, repository settings
- [docs/deployment.md](docs/deployment.md): beta and production, secrets, server and Cloudflare setup, rollbacks
- [docs/security-configuration.md](docs/security-configuration.md): settings, secrets and what is in place
- [docs/ai-development.md](docs/ai-development.md): the agent instructions and skills
- [docs/sonarqube.md](docs/sonarqube.md): optional weekly SonarQube Cloud analysis

# Security configuration

## Settings and secrets

The API reads its settings from `appsettings.json`, then environment variables (`Section__Key` for `Section:Key`).
Secrets never go in `appsettings*.json` or the repository.

| Setting | Required | Purpose |
| --- | --- | --- |
| `ConnectionString` | Outside Development | PostgreSQL connection string. Development falls back to the local database in `VoxaContextFactory`. |
| `APP_VERSION` | Set by the image | The release, reported by `/api/health` and the `X-App-Version` header. |
| `Cors__AllowedOrigins__<n>` | No | Origins that may call the API from a browser. Production sets the app's own host; add PR preview hosts here. Development allows every origin. |
| `ForwardedHeaders__KnownNetworks__<n>` | No | Extra proxy networks whose `X-Forwarded-*` headers are trusted, next to Docker's private ranges. |
| `Import__ApiKey` | No | Bearer key for the member import API ([import-api.md](import-api.md)). At least 32 characters; start-up fails on a shorter one. |

`Program.cs` refuses to start outside Development when a required secret is missing. Add every new secret to that
list, to this table and to `.env.api.example`.

In development, keep secrets out of the repository with user secrets:
`dotnet user-secrets --project Voxa.WebApi set "<Key>" "<value>"` (run `dotnet user-secrets init` once).

## What is already in place

- **HTTPS**: Traefik terminates TLS with a Let's Encrypt certificate (DNS challenge through Cloudflare) and redirects
  HTTP. The API sends HSTS outside Development; the Worker sends it on every page.
- **Forwarded headers**: the API trusts `X-Forwarded-For`/`-Proto` only from Docker's private networks and loopback, so
  a client cannot fake its address or scheme.
- **Content-Security-Policy** (`Voxa.PWA/src/worker.ts`): scripts only from the app itself, connections only to the
  app's origin (and the API's when `VITE_API_URL` points elsewhere), no framing by other sites, no plugins. Device APIs
  (camera, microphone, location, payment, USB) are switched off with `Permissions-Policy`. Unit tests pin these rules.
- **Member import API**: only `Authorization: Bearer <Import__ApiKey>` reaches `/api/import`, compared in constant
  time; the key reaches nothing else and the board's cookie does not reach the import API. Wrong keys are limited per
  client address (10 in 10 minutes). Without a key the API is closed.
- **Errors**: problem details without stack traces outside Development; unknown exceptions become a bare 500.
- **Containers**: the API runs as a non-root user; the database is only reachable on the internal network and the
  server's loopback; Traefik reads Docker through a read-only socket proxy; Watchtower's update endpoint needs a token
  and is rate limited.
- **CI**: actions pinned to commit SHAs, least-privilege `GITHUB_TOKEN` permissions, deploy secrets only in their
  GitHub environments, releases only from `main`.
- **Scanning**: CodeQL on every pull request and weekly, dependency review on pull requests, Dependabot updates for
  every ecosystem including the server's Compose images, and actionlint on the workflows
  ([continuous-integration.md](continuous-integration.md)).

## What a real project adds

- **Authentication and authorization.** Add it before the first endpoint that is not public, and deny by default
  (`[Authorize]` as a global filter, `[AllowAnonymous]` only where intended). Keep permission checks in logic.
- **Rate limiting** on sign-in and other endpoints that can be abused (`AddRateLimiter`). Behind Cloudflare, rate
  limits per client need the client address: add Cloudflare's ranges to `ForwardedHeaders__KnownNetworks` and to
  Traefik's `forwardedHeaders.trustedIPs`.
- **Backups** of the database volume, tested by restoring them.
- **Error reporting** for the API and the PWA.

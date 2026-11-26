# Deployment

Two environments, both on the same pattern: the PWA on a Cloudflare Worker, the API in Docker on a server behind
Traefik, and a single host name for both.

| | Beta | Production |
| --- | --- | --- |
| Started by | **Actions → Deploy beta → Run workflow** (any branch) | Publishing a GitHub release, then the next midnight (Europe/Amsterdam) |
| Workflows | `deploy-staging.yml` | `release-images.yml` (on publish), `publish-release.yml` (nightly) |
| Image tags | `beta`, `sha-<commit>` | `<version>`, `sha-<commit>`, and `latest` once deployed |
| Worker | `voxa-pwa-beta` | `voxa-pwa` |
| GitHub environment | `beta` | `production` |

Every deployment runs in this order:

1. Continuous integration (the same workflow as pull requests, all jobs).
2. Build and publish the Docker images to GitHub Container Registry.
3. **Update the API server** by calling Watchtower's HTTP API, then wait until `/api/health` reports the new version.
4. Build and deploy the PWA to Cloudflare Workers.
5. Check that the app's home page loads and `/api/health` still reports the new version. A failed check leaves the run
   red; see [Rolling back](#rolling-back).

Watchtower's update call is retried twice when it fails, and the API gets five minutes to report the new version
(it applies its migrations on start). A new beta run waits for the running one instead of cancelling it halfway.

The Worker goes last so a new front end never runs against an old API. For production, steps 1 and 2 run when the
release is published and steps 3 and 4 at night, so a release can be published during the day and goes live while
nobody is using the app. To deploy a release right away (a hotfix), run **Actions → Deploy published release → Run
workflow**, optionally with its tag.

## Releases

`continuous-release.yml` keeps a draft release up to date on every push to `main`, with the merged pull requests
grouped by label (`.github/release-drafter.yml`): `feature`/`enhancement` bump the minor version, `fix`/`bug`,
`documentation`, `chore`/`dependencies` the patch version, and `upgrade` the major version. Publish the draft to
release it. Release tags must point at a commit on `main`; the workflows refuse anything else.

## One-time setup

### GitHub

Under **Settings → Environments**, create `beta`, `production` and `preview`, and add:

| Name | Kind | Where | Value |
| --- | --- | --- | --- |
| `CLOUDFLARE_API_TOKEN` | secret | repository (or each environment) | Token with **Workers Scripts: Edit** |
| `CLOUDFLARE_ACCOUNT_ID` | secret | repository (or each environment) | The Cloudflare account id |
| `WATCHTOWER_BETA_TOKEN` | secret | `beta` | Same value as `WATCHTOWER_HTTP_API_TOKEN` on the beta server |
| `WATCHTOWER_TOKEN` | secret | `production` | Same value as `WATCHTOWER_HTTP_API_TOKEN` on the production server |
| `BETA_API_URL` | variable | `beta` | `https://<beta host>`, without a trailing slash |
| `PRODUCTION_API_URL` | variable | `production` | `https://<production host>` |
| `PREVIEW_API_URL` | variable | `preview` | The API the PR previews talk to, usually the beta host |

On `production`, add yourself as a required reviewer if releases should wait for a click before going live, and
restrict it to the `main` branch and `v*` tags. Leave `beta` open to every branch: trying a branch on beta is the
point of it.

The images are published to `ghcr.io/<owner>/<repository>/api`. Make the package public, or log the server in to
ghcr.io (see `docker-compose-production.yml`).

### Server

A Linux server with Docker and Docker Compose 2.24 or newer, one per environment.

1. Copy `docker-compose-production.yml` and `server.env.example` (as `.env`) to a folder on the server and fill in
   `.env`. For beta, set `APP_VERSION=beta`. Put API secrets in `.env.api` (see `.env.api.example`).
2. `docker compose -f docker-compose-production.yml up -d`.
3. The API applies its migrations when it starts. Check `https://<host>/api/health`.

Allow only ports 80 and 443 in, ideally only from [Cloudflare's ranges](https://www.cloudflare.com/ips/), so nobody
bypasses Cloudflare.

### Cloudflare

In the zone of the app's host:

1. A proxied DNS record for the host pointing at the server, and SSL/TLS mode **Full (strict)**.
2. **Workers Routes**: `<host>/*` → the Worker (`voxa-pwa` or `voxa-pwa-beta`), and `<host>/api/*` → **None**.
   The more specific route wins, so `/api` reaches the server. Without the `None` route the Worker answers every API
   request with a 404.
3. Deploy each Worker once (run the deploy workflow) before the PR previews can upload versions of it. Enable
   **Preview URLs** on the Worker for the `pr-<n>` aliases.

## Rolling back

- **API**: set `APP_VERSION` in the server's `.env` to the previous version and run
  `docker compose -f docker-compose-production.yml up -d api`. Migrations are not rolled back, so keep them
  backwards compatible (add, then remove in a later release).
- **PWA**: **Workers & Pages → the Worker → Deployments → Rollback**, or run **Deploy published release** with the
  previous tag (it also moves the API back).

## Renaming the Workers

The Worker names come from `Voxa.PWA/wrangler.jsonc` (production) and the `worker:` entries in
`deploy-staging.yml` and `publish-release.yml`. `tools/init/rename.mjs` renames all of them together; when renaming by
hand, change them together and update the Workers Routes.

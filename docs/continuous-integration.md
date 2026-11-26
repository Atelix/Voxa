# Continuous integration and auto-merge

The `Continuous integration` workflow runs on every pull request, in the merge queue, before every beta deployment and before the images of a published release are built.

## What runs

| Job | Runs when |
| --- | --- |
| Load settings | Always |
| Build and test .NET solution | Anything outside `Voxa.PWA/`, `docs/` and root `*.md` files changed |
| Build .NET solution with analyzers | Same as the .NET build, and runs next to it; the test job builds without analyzers |
| Run PostgreSQL tests | Same as the .NET build, and runs next to it |
| Check .NET formatting | Same as the .NET build, and runs next to it |
| Build, type-check and lint frontend | Anything in `Voxa.PWA/` (or `.node-version`) changed |
| Build Docker `<image>` | A `Dockerfile`, `.dockerignore`, `docker-compose*.yml` or `.github/CICD-Config.yml` changed and the other checks passed |
| CI result | Always |

The .NET formatting job checks C# formatting with `dotnet format whitespace` and `dotnet format style` against the root `.editorconfig`, for `Voxa.sln` and `Voxa.WebApi.PostgresTests`. EF Core migrations are generated code and are left out. Analyzer warnings (the `CA` rules) are not part of the check; they stay build warnings. When the check fails, fix it locally and commit the result:

```bash
dotnet format whitespace Voxa.sln --exclude Voxa/Migrations
dotnet format style Voxa.sln --exclude Voxa/Migrations
```

Run the same two commands with `Voxa.WebApi.PostgresTests` instead of `Voxa.sln` when you changed that project. Plain `dotnet format` also applies analyzer code fixes, which can generate code (for example empty method stubs), so use the two subcommands instead.

The frontend lint is type-aware: ESLint uses `typescript-eslint`'s `recommendedTypeChecked` rules with the same `tsconfig.app.json` that `type-check` uses, plus `tsconfig.node.json` for the Vite and Vitest config files. It catches promises that are neither awaited nor handled (`no-floating-promises`, `no-misused-promises`) and values typed `any`. Await a promise where the code can wait for it. A deliberate fire-and-forget call, such as a navigation or a reload from an event handler, is written as `void promise`. Types exported from a `.vue` file are invisible to ESLint, so shared types and the shapes of exposed component methods live in `.ts` files.

The .NET job also runs `Voxa.WebApi.PostgresTests` against a throwaway PostgreSQL container. That project is not in `Voxa.sln`, so `dotnet test Voxa.sln` needs no Docker locally.

Changes to `.github/` run the .NET and frontend jobs; they build the Docker images only when `.github/CICD-Config.yml` changed. Code changes alone do not build the images, so a change that breaks an image build without touching a Docker or Compose file shows up when beta is deployed or a release is published. Deployments, releases and manual runs always run every job, except that beta deployments and release image builds skip **Build Docker**: their publish job builds the same images right after the tests pass, so building them in CI as well would only double the work.

Docker builds do not use the GitHub Actions layer cache. Importing and exporting that cache took longer than rebuilding the images.

## Other checks

- **Load settings** also runs `node tools/ai/sync-skills.mjs --check` (the agent skills are in sync, see
  [ai-development.md](ai-development.md)) and the tests of the repository's own tools (`node --test "tools/**/*.test.mjs"`).
- **PWA preview** (`preview.yml`) uploads a preview version of the Worker for pull requests that change the PWA and
  comments its URL. It is not part of `CI result`.
- **SonarQube** (`sonarqube.yml`) runs weekly once configured; see [sonarqube.md](sonarqube.md).

## Workflow security

- Every action from another repository is pinned to a full commit SHA, with the release it came from in a comment (`uses: actions/checkout@<sha> # v7.0.1`). A moved or hijacked tag can therefore not change what runs. Dependabot's monthly update changes the SHA and the comment together (`.github/dependabot.yml`); pin a new action the same way, for example with `git ls-remote https://github.com/<owner>/<action> refs/tags/<tag>` (use the `^{}` line for an annotated tag).
- Each workflow declares the least `GITHUB_TOKEN` permissions it needs. CI only reads the repository; only the jobs that push images get `packages: write`.
- The deploy jobs run in the GitHub environments `production` (Deploy published release) and `beta` (Deploy beta). Secrets stored on an environment are only given to jobs of that environment, and the environment's protection rules (required reviewers, allowed branches or tags) apply before such a job starts. Until a secret is moved to an environment, the jobs keep reading the repository secret of the same name. The Cloudflare token is passed only to the `wrangler` step, not to the install and build steps.
- Publish release images and Deploy published release refuse a release whose tag does not point at a commit on `main`.

## CI result

`CI result` is the only status check that branch protection and auto-merge need. It passes only when every job that was needed for the change succeeded and every other job was skipped. It fails when a needed job failed, was cancelled, or did not run. Requiring the individual jobs instead is not recommended: jobs skipped for unchanged code would still have to be listed, and a new job could be forgotten.

## Repository settings

These settings are made in GitHub by a repository administrator. The ruleset is also stored as
`.github/rulesets/protect-main.json`: import it under **Settings → Rules → Rulesets → New ruleset → Import a ruleset**
instead of clicking it together (it also requires a code-owner approval, see `.github/CODEOWNERS`).

1. **Settings → General → Pull Requests**: enable **Allow auto-merge**.
2. **Settings → Rules → Rulesets → New branch ruleset**, targeting the default branch (`main`):
   - Enable **Require a pull request before merging**.
   - Enable **Require status checks to pass** and add the check **CI result** from the source **GitHub Actions**.
   - Enable **Require branches to be up to date before merging** so a pull request is tested against the latest `main` before it merges. When `main` moves ahead, select **Update branch** on the pull request; auto-merge stays enabled and merges once the new run passes.
   - Enable **Block force pushes** and **Restrict deletions**.
3. Optional: enable **Require merge queue** in the same ruleset. The queue retests pull requests together with the latest `main` and merges them without a manual branch update. Merge queues are only available for some organization plans.

## Using auto-merge

On a pull request, select **Enable auto-merge** (or run `gh pr merge --auto --squash <number>`). GitHub merges the pull request as soon as `CI result` has passed and every other rule, such as required reviews, is satisfied. When a new commit is pushed, the checks run again and the merge waits for the new result. When a check fails, the pull request stays open.

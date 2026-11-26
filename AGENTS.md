# Repository instructions

These instructions apply to the entire repository and must be followed whenever code is created or changed.

## ECC engineering foundation

This repository uses a pinned selection of [ECC](https://github.com/affaan-m/ECC) as the foundation
for Codex and Claude Code. Codex discovers `.agents/skills/`; Claude loads `CLAUDE.md`, which imports
this file, and discovers `.claude/skills/`. Read only the skills relevant to the current task.

- Research existing project code and primary documentation before adding a feature, utility or dependency
  (`ecc-search-first`). Read `docs/architecture.md` when crossing project boundaries.
- For multi-step work, make a concise plan before editing. Use the existing `writing-plans` and
  `brainstorming` skills when their scope fits, and `issues-are-suggestions` when starting from an issue.
- For behavior changes, reproduce the problem or write meaningful tests, implement the smallest fix,
  then refactor (`ecc-tdd-workflow`). Use `systematic-debugging` for failures.
- Apply `ecc-dotnet-patterns` and `ecc-csharp-testing` for C# work, `ecc-vue-patterns` and
  `material-design` for visual UI work, `api-client` for API design and contracts, and
  `ecc-database-migrations`/`ecc-postgres-patterns` for schema and query changes.
- Review the diff for correctness, error handling, data isolation, authorization and sensitive-data
  exposure. Use `ecc-security-review` when changing authentication, permissions, uploads, webhooks or
  other sensitive behavior.
- Before reporting completion or opening a PR, use `verification-before-completion`, which owns
  the ECC verification workflow; run the full validation suite below and report actual results.
  Validate changes to agent instructions, skills or their tooling with
  `node tools/ai/sync-skills.mjs --check` as well.

These project instructions, `.editorconfig`, existing project conventions and the user's requested scope
govern generic ECC examples. Use pnpm and the versions specified by `.node-version` and the PWA's
`packageManager` field. Do not introduce sample frameworks, missing ECC scripts, new coverage gates,
global tool settings or unrelated architectural rewrites to satisfy an upstream example.
Details and upstream provenance are in `docs/ai-development.md` and `docs/ai/ecc/manifest.json`.

## Definition of valid code

A code change is valid only when every relevant build, test, type-check, and lint command completes successfully. Do not present code as complete or valid while any required check is failing.

Before finishing a code change, run all of the following from the repository root:

```bash
dotnet build Starter.sln
dotnet test Starter.sln
dotnet format whitespace Starter.sln --verify-no-changes --exclude Starter/Migrations
dotnet format style Starter.sln --verify-no-changes --exclude Starter/Migrations
pnpm --dir Starter.PWA run type-check
pnpm --dir Starter.PWA run lint
pnpm --dir Starter.PWA run test
pnpm --dir Starter.PWA run build
```

When a change touches the database (entities, `StarterContext`, migrations or queries whose behaviour depends on
PostgreSQL), also run `dotnet test Starter.WebApi.PostgresTests` (needs Docker; see its README).

When a change affects only one part of the repository, focused checks may be run during development, but the full validation suite above is required before completion.

## Failure handling

- Fix every build error, failing test, TypeScript error, and ESLint error caused by or exposed by the change.
- Do not disable tests, weaken compiler settings, suppress lint rules, or add ignore directives merely to make validation pass.
- Do not change existing tests solely to conceal a regression. Update tests only when the intended behavior has legitimately changed.
- Do not claim that checks passed unless they were actually run and completed successfully.
- If a required command cannot be run because of an environmental or dependency problem, clearly report the command, the blocker, and which checks remain unverified.

## Implementation expectations

- Preserve the repository's existing architecture, conventions, formatting, and public behavior unless the task requires a change.
- Add or update tests for changed behavior and bug fixes where practical.
- Keep changes focused on the requested task; do not modify unrelated files.
- Review the final diff for accidental changes before reporting completion.
- Define service and logic interfaces in `Starter.Abstractions` rather than alongside implementations. Keep each interface in its own file and place shared request/response contracts in `Starter.Abstractions/Models`.
- Interface methods return models, not EF entities, and never take `HttpContext`. Implementations that need the request use `IHttpContextAccessor`. Keep visibility and redaction rules in logic, not in controllers.
- Report failures by throwing the domain exceptions in `Starter.Abstractions` (`ObjectNotFoundException`, `DomainValidationException`, `DomainForbiddenException`, `DomainConflictException`); `DomainExceptionHandler` turns them into problem responses. Do not build error responses in controllers.
- Environment variables and `appsettings` are for secrets, addresses and infrastructure. Keep product behaviour (feature switches, limits, retention periods) in code or the database so it is reviewed and tested like other behaviour.
- Every schema change is an EF Core migration (`dotnet ef migrations add <Name> --project Starter --startup-project Starter.WebApi`). Review the generated migration and snapshot; `MigrationTests` fails when the model changed without one.

## Working with large generated files

- The generated API client (`Starter.PWA/src/logic/api.ts`) grows with the API. Search it for the type or method you need instead of reading it whole, and review regenerated changes with `git diff`.
- Lockfiles, `node_modules`, `dist`, `bin` and `obj` are not for reading; `.claude/settings.json` blocks Claude from opening them.

## PWA loading states

- Use Vuetify's `v-skeleton-loader` for every loading state that stands in for content (pages, cards, lists, tables, dialogs, previews). Do not use `v-progress-circular` or `v-progress-linear` as a content placeholder.
- Choose skeleton `type`s that mirror the layout that will replace them (for example `list-item-avatar-two-line@5` for a member list, `heading, paragraph` for an article), so the page does not shift when data arrives.
- For `v-data-table` and `v-data-table-server`, keep the `:loading` prop and add a `#loading` slot containing `<v-skeleton-loader type="table-row@N" />`.
- Track loading with an explicit flag that is reset in a `finally` block, and render the skeleton instead of empty-state components such as `k-notfound` until the first response arrives. Never show an empty or "not found" state while data is still loading.
- In `Starter.PWA`, load view data with `useLoader` (`src/services/useLoader.ts`), whose `loading` starts true and ignores stale responses, and run saves with `useAsyncAction` (`src/services/useAsyncAction.ts`). Use them in new views and in views you touch.
- When reloading data that is already displayed (for example after saving), it is fine to keep the current content visible instead of swapping it for a skeleton.
- Spinners remain appropriate only for in-place action feedback: `:loading` on buttons, inputs and autocompletes, and small inline status messages.

## PWA API endpoints

- Create and update PWA API endpoints from the Web API's Swagger schema by using the `generate-api-schema` script in `Starter.PWA/package.json`; do not manually create or edit generated endpoint definitions.
- Before generating endpoints, run the Web API in Development so that its Swagger document is available at `http://localhost:5143/swagger/v1/swagger.json`.
- From the repository root, generate the PWA API client with:

```bash
pnpm --dir Starter.PWA run generate-api-schema
```

- Run this command whenever controllers, routes, request models, response models, or other API-contract types change.
- Review the generated changes in `Starter.PWA/src/logic/api.ts` and include them with the API-contract change.
- Mark response-model properties the API always sends as `required`, so the generated types do not make them optional.
- After generation, run the complete validation suite from the "Definition of valid code" section.

## Translations

- All user-facing text in the PWA goes through vue-i18n (`$t(...)`), with keys in `src/languages/en.ts` and `nl.ts`. Add every key to both files; a unit test fails when they differ.

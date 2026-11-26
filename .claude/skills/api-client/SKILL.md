---
name: api-client
description: Use when designing or changing the project's API controllers, routes, request/response contracts or generated client. Owns API design, authorization boundaries, Swagger regeneration, API docs and contract validation, using the ECC API-design reference.
---

# API development and the generated client

`AGENTS.md` is the source of truth; this skill is the checklist that applies it.
The front end talks to `Voxa.WebApi` through a generated client that must never
be edited by hand:

| Client | Generated file | Swagger document | Covers |
|--------|----------------|------------------|--------|
| PWA | `Voxa.PWA/src/logic/api.ts` | `/swagger/v1/swagger.json` | every controller |

When you add a second front end (for example an admin app), give it its own Swagger
document and its own generated client rather than sharing one, and add a row here.

## Contract design with ECC

Read [ECC API design](../../../docs/ai/ecc/skills/api-design/SKILL.md) for the relevant
contract-design principles. Apply them through the project's existing routing, `ApiResponse<T>`
envelopes, domain exceptions and problem details. Generic Next.js, Django, Go and versioning
examples do not replace the project's architecture or justify renaming unrelated endpoints.

Read `docs/architecture.md` for how a request flows. Treat route ids, headers and request
bodies as untrusted until verified, keep visibility and redaction rules in logic (not in
controllers), and cover denied-access cases when changing permissions or exposing personal data.

## 1. Make the backend change

- Service and logic interfaces go in `Voxa.Abstractions`, one interface per file. Shared
  request/response contracts go in `Voxa.Abstractions/Models`, not next to the controller.
- Interface methods return models, never EF entities, and never take `HttpContext`.
- Failures are domain exceptions (`ObjectNotFoundException`, `DomainValidationException`,
  `DomainForbiddenException`, `DomainConflictException`); `DomainExceptionHandler` turns them
  into problem responses. Don't build problem responses in controllers.
- Add or update unit tests in `Voxa.WebApi.UnitTests` for the changed behavior.

## 2. Regenerate the client

1. Start the API in Development so Swagger is served on port 5143. Swagger needs no
   database; from the repository root:
   ```bash
   ASPNETCORE_ENVIRONMENT=Development dotnet run --project Voxa.WebApi --no-launch-profile --urls http://localhost:5143
   ```
   Run it in the background and wait until the next step succeeds (about 20 s).
2. Check the document responds: `curl -sf http://localhost:5143/swagger/v1/swagger.json >/dev/null`.
3. From the repository root: `pnpm --dir Voxa.PWA run generate-api-schema`.
4. Stop the API.

If the API cannot start, stop and say so. Do not hand-edit `api.ts` to make the front end compile.

## 3. Review the generated diff

- Read `git diff -- '*/src/logic/api.ts'`. It should contain only the contract change you
  made. Unrelated churn usually means your local API is on a different branch or
  configuration; fix that rather than committing the churn.
- Renamed or removed types and methods break callers: search the app for the old names and
  update them.

## 4. Validate

Run the full suite from `AGENTS.md` ("Definition of valid code") and report the actual
results. Use the `verification-before-completion` skill before saying the change is done.

# Coding with Codex and Claude

This project uses [ECC](https://github.com/affaan-m/ECC) as its engineering foundation: research existing
solutions, plan the change, test the intended behavior, implement, review and verify. This is a
repository-scoped selection of ECC's instruction-only workflows, adapted to the template's .NET and Vue stack.

## Shared instructions and skill discovery

- **Codex** reads the root `AGENTS.md` and discovers project skills in `.agents/skills/`.
- **Claude Code** reads the root `CLAUDE.md`, which imports `AGENTS.md`, and discovers skills in
  `.claude/skills/`.
- Each skill has one canonical instruction body. ECC-specific bodies live under `.agents/skills/`,
  with small Claude discovery bridges. Project-owned bodies live under `.claude/skills/`, with
  small Codex discovery bridges. Both tools expose the same 16 distinct project skills.
- Eight `ecc-*` skills link to unmodified ECC references under `docs/ai/ecc/skills/`. ECC API design
  and verification are integrated into `api-client` and `verification-before-completion`, so those
  responsibilities have one active entry each.
- Material Design 3 is reference material inside `material-design`, including its original license
  and reference documents. It is no longer a second active design skill.

Start either tool in the repository. There is no extra package installation for these skills.
Use current versions of Codex and Claude Code that support project skill discovery.

| Responsibility | Active skill |
| --- | --- |
| New feature, utility, dependency or integration research | `ecc-search-first` |
| Behavior changes and regression tests | `ecc-tdd-workflow` |
| Required checks and evidence before a completion claim or PR | `verification-before-completion` |
| Authentication, authorization, sensitive data, uploads and webhooks | `ecc-security-review` |
| C# logic, services and dependency injection | `ecc-dotnet-patterns` |
| .NET unit and database tests | `ecc-csharp-testing` |
| Vue reactive logic, composables, Pinia, routing and request lifecycle | `ecc-vue-patterns` |
| API design, contracts, Swagger clients and API documentation | `api-client` |
| EF Core schema changes | `ecc-database-migrations` |
| PostgreSQL queries, indexes and performance | `ecc-postgres-patterns` |
| Product intent and design decisions | `brainstorming` |
| Step-by-step implementation plans | `writing-plans` |
| Interpreting an issue against the project's architecture | `issues-are-suggestions` |
| Root-cause investigation for failures | `systematic-debugging` |
| Visual UI, Vuetify, branding, accessibility and MD3 audits | `material-design` |
| Real browser behavior at phone and desktop widths | `webapp-testing` |

Research, design, implementation planning and debugging have different responsibilities. Likewise,
TDD owns the testing process while C# testing owns .NET test fixtures and assertions; Vue patterns
own application logic while Material Design owns presentation. Load the skills needed for the task,
without running two workflows for the same responsibility.

Modern Web Guidance (Google Chrome's `modern-web-guidance` plugin) is enabled for Claude Code in
`.claude/settings.json`. It adds current browser-platform guidance (HTML, CSS, accessibility,
performance) next to these skills; Claude Code asks once to trust the marketplace.

Project rules, `.editorconfig`, existing architecture and the user's requested scope govern the
generic ECC examples. In particular, use pnpm, preserve the project's authorization and response contracts,
regenerate Swagger clients, use Vuetify loading skeletons, and run the full suite in `AGENTS.md`.
ECC's sample frameworks, scripts, slash commands and agent names are references; this selection does
not provide their runtimes. Keep the existing Claude session setup. Hooks, MCP servers, model choices,
permissions and persistent learning from the complete ECC plugin are outside this foundation.

## Provenance

The imported references are pinned to ECC **2.2.3**, commit
[`b92f181b3b748d1abbc2afd94af6aa7d8c8fca29`](https://github.com/affaan-m/ECC/tree/b92f181b3b748d1abbc2afd94af6aa7d8c8fca29).
`docs/ai/ecc/manifest.json` records the upstream repository, revision, source paths, SHA-256 hashes
and project adaptations. The upstream MIT license is retained in `docs/ai/ecc/LICENSE`.

Keep imported references byte-for-byte identical to the pinned source. Edit the adaptations in the
manifest rather than editing generated adapters. Maintain the integrated API/verification guidance
in its canonical the project skill. Each imported ECC reference is stored once.

## Maintaining the foundation

From the repository root:

```bash
# Regenerate adapters after changing the ECC manifest or an existing Claude skill's metadata.
node tools/ai/sync-skills.mjs --write

# Verify upstream hashes, adapters, imports, unique skill identities and catalog parity.
node tools/ai/sync-skills.mjs --check
```

The second command also runs in the existing CI configuration job. The script uses Node's standard
library and works without network access, dependencies or symbolic links. It rejects duplicate names
or descriptions within either tool's catalog, differing tool catalogs and the retired overlapping
entries `ecc-api-design`, `ecc-verification-loop` and `material-3`. It only writes its generated
adapters with `--write`; it never changes existing Claude skills, global tool settings or upstream files.

To update ECC:

1. Check out a specific revision of the official ECC repository and review the selected workflows.
2. Copy the selected Markdown references and the MIT license to their existing destinations.
3. Update the version, revision and SHA-256 hashes in the manifest; revise project adaptations if needed.
4. Regenerate adapters, run the integrity check and review the diff. If removing a workflow, remove
   its generated adapters and vendored reference explicitly.
5. Run the validation suite in `AGENTS.md` and open a PR explaining the upstream changes.

## Checking discovery

Start a fresh Codex or Claude Code session at the repository root and ask it to list its active
repository instructions and project skills. It should report `AGENTS.md` (imported through
`CLAUDE.md` for Claude) and the 16 distinct skills above, including eight `ecc-*` entries.
Invoke `$ecc-search-first` in Codex or `/ecc-search-first` in Claude on a small research task.
This checks actual discovery in the installed tool; the CI integrity check validates the files.

Discovery formats are documented by
[OpenAI](https://developers.openai.com/codex/skills) and
[Claude Code](https://code.claude.com/docs/en/skills); Claude's
[instruction imports](https://code.claude.com/docs/en/memory) keep project policy shared.

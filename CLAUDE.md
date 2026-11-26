# Claude Code instructions

@AGENTS.md

The imported repository instructions are the shared foundation for Claude Code and Codex.
Use the task-relevant skills in `.claude/skills/`; ECC-specific skills have the `ecc-` prefix.
API design and verification live in `api-client` and `verification-before-completion`.
Each discovery entry points to one canonical instruction body; avoid repeating the same workflow.
Read `docs/ai-development.md` when maintaining this setup.

Keep the existing session-start hook, plugins and read permissions in `.claude/settings.json`.
Load only the skills and reference sections needed for the current task.

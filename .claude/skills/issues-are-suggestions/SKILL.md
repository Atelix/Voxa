---
name: issues-are-suggestions
description: Use when picking up a GitHub issue, feature request or bug report for the project, before designing or building anything. Issues are suggestions, not specs; work out the solution that fits the project best.
---

# Issues are suggestions, not specs

An issue in the project describes a need, a symptom or someone's first idea for a
fix. It is not a specification. The wording, the proposed endpoint, the field
names and the screen it mentions are the author's suggestion of *one* way to
meet the need. Your job is to understand the need and propose the solution
that fits the project best, which may be what the issue says, a variant of it, or
something different.

Do not implement an issue's wording literally just because it is written down.

## Why this matters

Example: an issue asks for a "delete" button on a list of records, and it is
built as written. But the project already archives those records elsewhere,
and other features rely on them still existing. Reading the issue against how
the project already works would have caught that before any code was written.

## Process

1. **Read the issue for the need.** Separate *what problem the author has*
   (who is blocked, what they are trying to do, what goes wrong) from *what
   they propose*. Write the need down in one or two sentences in your own words.
2. **Read the project before designing.** Check how the area works today:
   - the existing code and the flow the issue touches;
   - `docs/` (for example `architecture.md`)
     for decisions and boundaries already made;
   - `AGENTS.md` and the other skills in `.claude/skills/`;
   - related issues and merged PRs, which often record why something is the
     way it is.
3. **Check the suggestion against the project.** Ask:
   - Does it respect existing boundaries (roles and permissions, which app or
     endpoint owns the feature, who may see which data)?
   - Does the project already have a feature, endpoint or pattern that covers this,
     or that the change should extend instead of duplicating?
   - Does it fit the existing architecture, naming and conventions?
   - Is it the smallest change that meets the need, or does it add surface
     nobody asked for?
   - Does it have side effects the author may not have considered (security,
     privacy, notifications or emails to members, data loss)?
4. **Decide on the best fit.** Pick the solution that meets the need and fits
   The project. When it differs from the issue, that is expected and fine.
5. **Say how you read it.** When you propose or open a PR, state briefly:
   - the need, in your words;
   - what you are building;
   - where and why it differs from the issue's wording (or that it follows
     the issue as written, because that already fits).
   When the difference changes what users see, what other people depend on,
   or a boundary such as who is allowed to do something, put it to the owner
   before building rather than deciding silently.
6. **Then design and build** with the normal skills: `brainstorming` for new
   features or behavior changes, `systematic-debugging` for bugs, `api-client`
   for API contract changes, `verification-before-completion` before claiming
   it is done.

## Bug reports

A bug report's description of the cause and its suggested fix are guesses too.
Reproduce the problem and find the root cause (see `systematic-debugging`);
fix the cause, which may be somewhere other than where the report points.

## Red flags

| Thought | Reality |
|---------|---------|
| "The issue says exactly what to build, so I'll build that" | The issue is a suggestion. Check it against the project first. |
| "Changing the design would ignore the author" | Meeting the author's need in the way that fits the project serves them better. Explain the difference. |
| "The issue asks for a new endpoint/page/setting" | Check whether an existing one should be extended, or whether the feature belongs elsewhere. |
| "It is only one extra right/permission" | Rights and boundaries are design decisions. Check who should have it before adding it. |
| "I'll mention the difference after it is built" | Differences that change behavior or boundaries are raised before building. |

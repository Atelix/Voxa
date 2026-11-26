# SonarQube Cloud

The `SonarQube` workflow (`.github/workflows/sonarqube.yml`) analyses `main` once a week in SonarQube Cloud and turns the open findings into GitHub issues. It analyses the C# code, with coverage from the unit tests. Pull requests are not analysed, and the workflow is not part of `CI result` (see [continuous-integration.md](continuous-integration.md)), so SonarQube never blocks a merge or a deployment.

## Plan

SonarQube Cloud's Free plan allows 50,000 private lines of code and five members per organization, analyses `main` and pull requests into it, and offers only the built-in **Sonar way** quality profile and gate. This template uses only the `main` analysis.

Only C# is analysed (`sonar.inclusions=**/*.cs`), which keeps a growing project under that limit for longer. The PWA already gets type-aware ESLint in CI. To analyse it as well, drop the inclusion, install the PWA dependencies before the scan, and exclude the generated `src/logic/api.ts` client.

The workflow is skipped until the `SONAR_PROJECT_KEY` variable is set, so a new project made from the template stays green without SonarQube.

## Setup

These steps are done by an organization administrator and are not stored in the repository.

1. In SonarQube Cloud, import the GitHub organization and create a **private** project bound to `<owner>/<repository>`. Turn off **Automatic Analysis**, because this workflow replaces it. Make sure the project's main branch is named `main` (**Administration → Branches and Pull Requests**); see [Troubleshooting](#troubleshooting).
2. Create a project analysis token and store it in GitHub under **Settings → Secrets and variables → Actions → Secrets** as `SONAR_TOKEN`.
3. Under **Actions → Variables**, add `SONAR_ORGANIZATION` (the organization key) and `SONAR_PROJECT_KEY` (the project key).
4. Optionally add `SONAR_ISSUES_MIN_SEVERITY` and `SONAR_ISSUES_MAX_NEW`; see [Issues](#issues).
5. Run **Actions → SonarQube → Run workflow** on `main` with **dry run** ticked to see which issues it would open, then once without it.

## When it runs

- Every Monday at 04:23 UTC (`schedule`).
- On request under **Actions → SonarQube → Run workflow**, on `main` only. A run started on another branch is skipped.

## What is analysed

The workflow runs SonarScanner for .NET (pinned as `SONAR_SCANNER_VERSION`) from the repository root:

1. `dotnet-sonarscanner begin` with the project and exclusion settings.
2. `dotnet build Voxa.sln --no-incremental`.
3. `dotnet test Voxa.sln` with the Coverlet collector writing OpenCover reports, imported through `sonar.cs.opencover.reportsPaths`.
4. `dotnet-sonarscanner end`, which uploads the analysis.

The scanner swaps in its own ruleset and turns off `WarningsAsErrors` for its instrumented build, so this never replaces the normal CI build.

`Voxa.WebApi.UnitTests` counts as test code because it is an `IsTestProject` project. Excluded: `bin/`, `obj/` and the generated `Voxa/Migrations`.

## Issues

After the analysis has been processed, `tools/sonarqube/sync-issues.mjs` reads the open issues and the security hotspots to review from SonarQube Cloud and keeps one GitHub issue per rule:

- **New rule with findings**: opens an issue titled `SonarQube: <rule name>`, with the findings listed and linked to the analysed commit (at most 50 per issue; the rest are linked in SonarQube Cloud). Bugs and vulnerabilities get the issue type Bug and the `bug` label, everything else the type Task and the `chore` label. Vulnerabilities and security hotspots also get `security`, and the language adds `.NET`, `javascript`, `github_actions` or `docker`. Only labels that already exist are used.
- **Rule already has an open issue**: updates its list of findings when it changed.
- **Rule has no open findings any more**: comments and closes the issue as completed.
- **Findings come back** after the issue was closed as completed: reopens it.
- **Issue closed as not planned**: stays closed. Mark the findings as accepted or false positive in SonarQube Cloud so they stop being reported.

Each issue has a hidden `<!-- sonarqube-rule: <key> -->` marker that links it to its rule; keep it when editing the issue. Findings below `SONAR_ISSUES_MIN_SEVERITY` (`INFO`, `LOW`, `MEDIUM`, `HIGH` or `BLOCKER`; default `MEDIUM`) are left out. At most `SONAR_ISSUES_MAX_NEW` new issues (default 25) are opened per run, most severe first, so a first run does not flood the issue list; the remaining rules follow in later runs. The run summary lists every rule and what was done.

Run the script's tests with `node --test "tools/sonarqube/*.test.mjs"`.

## Troubleshooting

- **"The analysis went to the branch "main", but the main branch of the SonarQube Cloud project is …"**: the project's main branch has another name (a new project often starts with `master`). The scanner then files each analysis under a separate short-lived branch `main`, which only shows findings in changed code, while the issues are read from the main branch, whose findings stay frozen at an old analysis. In SonarQube Cloud, under **Administration → Branches and Pull Requests**, delete the branch `main`, rename the main branch to `main`, and run the workflow again.
- **"SonarQube is not configured"**: the secret or one of the variables above is missing.
- **"Something went wrong while trying to get the pullrequest"**: a pull request analysis was attempted; this workflow never does that. Check that no other workflow or Automatic Analysis is running.
- **"SonarQube Cloud did not finish processing the analysis in time"**: the sync step waits up to five minutes. Check the SonarQube Cloud status page and re-run the workflow.
- **"…reach the maximum allowed lines limit of 50000"**: the analysis counts more than the Free plan allows. Check the language distribution in the error and leave out what should not be analysed.
- **Coverage is 0% for C#**: check that the test step wrote a `coverage.opencover.xml` attachment.

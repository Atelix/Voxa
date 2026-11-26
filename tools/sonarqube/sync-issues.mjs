// Turns the open findings of the latest SonarQube Cloud analysis of main into GitHub issues, one issue per rule.
// Run by .github/workflows/sonarqube.yml after the analysis has been processed; see docs/sonarqube.md.
//
// Each issue carries a hidden marker with the rule key. A later run updates the issue's list of findings,
// closes it when the rule has no open findings left, and reopens it when findings come back. An issue that
// was closed as "not planned" stays closed: dismiss the findings in SonarQube Cloud to stop reporting them.

export const severities = ["INFO", "LOW", "MEDIUM", "HIGH", "BLOCKER"];

const legacySeverities = { INFO: "INFO", MINOR: "LOW", MAJOR: "MEDIUM", CRITICAL: "HIGH", BLOCKER: "BLOCKER" };
const typeByQuality = { SECURITY: "VULNERABILITY", RELIABILITY: "BUG", MAINTAINABILITY: "CODE_SMELL" };
const typeNames = { VULNERABILITY: "Vulnerability", BUG: "Bug", SECURITY_HOTSPOT: "Security hotspot", CODE_SMELL: "Code smell" };
const typeRank = { VULNERABILITY: 0, BUG: 1, SECURITY_HOTSPOT: 2, CODE_SMELL: 3 };
const markerPattern = /<!-- sonarqube-rule: (\S+) -->/;
const maxListedFindings = 50;

export function marker(rule) {
    return `<!-- sonarqube-rule: ${rule} -->`;
}

export function ruleFromBody(body) {
    return markerPattern.exec(body ?? "")?.[1] ?? null;
}

function rank(severity) {
    return severities.indexOf(severity);
}

// The highest impact severity of a SonarQube issue, falling back to the legacy severity.
export function severityOf(issue) {
    const impacts = (issue.impacts ?? []).map(impact => impact.severity).filter(severity => rank(severity) >= 0);
    if (impacts.length > 0) {
        return impacts.reduce((highest, severity) => (rank(severity) > rank(highest) ? severity : highest));
    }
    return legacySeverities[issue.severity] ?? "MEDIUM";
}

export function typeOf(issue) {
    if (typeNames[issue.type]) {
        return issue.type;
    }
    const quality = issue.impacts?.[0]?.softwareQuality;
    return typeByQuality[quality] ?? "CODE_SMELL";
}

function pathOf(component, projectKey) {
    const prefix = `${projectKey}:`;
    return component?.startsWith(prefix) ? component.slice(prefix.length) : (component ?? "");
}

// Normalises issues and hotspots into findings and keeps those at or above the minimum severity.
export function toFindings({ issues, hotspots, projectKey, minSeverity }) {
    const findings = [
        ...issues.map(issue => ({
            rule: issue.rule,
            type: typeOf(issue),
            severity: severityOf(issue),
            path: pathOf(issue.component, projectKey),
            line: issue.line ?? issue.textRange?.startLine ?? null,
            message: issue.message ?? "",
        })),
        ...hotspots.map(hotspot => ({
            rule: hotspot.ruleKey,
            type: "SECURITY_HOTSPOT",
            severity: rank(hotspot.vulnerabilityProbability) >= 0 ? hotspot.vulnerabilityProbability : "MEDIUM",
            path: pathOf(hotspot.component, projectKey),
            line: hotspot.line ?? hotspot.textRange?.startLine ?? null,
            message: hotspot.message ?? "",
        })),
    ];
    return findings.filter(finding => finding.rule && rank(finding.severity) >= rank(minSeverity));
}

// Groups findings per rule, most severe and most frequent first.
export function groupByRule(findings) {
    const groups = new Map();
    for (const finding of findings) {
        let group = groups.get(finding.rule);
        if (!group) {
            group = { rule: finding.rule, type: finding.type, severity: finding.severity, findings: [] };
            groups.set(finding.rule, group);
        }
        if (rank(finding.severity) > rank(group.severity)) {
            group.severity = finding.severity;
        }
        if (typeRank[finding.type] < typeRank[group.type]) {
            group.type = finding.type;
        }
        group.findings.push(finding);
    }
    for (const group of groups.values()) {
        group.findings.sort((a, b) => a.path.localeCompare(b.path) || (a.line ?? 0) - (b.line ?? 0));
    }
    return [...groups.values()].sort((a, b) =>
        rank(b.severity) - rank(a.severity)
        || typeRank[a.type] - typeRank[b.type]
        || b.findings.length - a.findings.length
        || a.rule.localeCompare(b.rule));
}

// Rule messages are SonarQube text; keep them from rendering HTML or mentioning people.
function escape(text) {
    return text.replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll("@", "&#64;");
}

function languageOf(rule) {
    return rule.split(":")[0];
}

export function labelsFor(group) {
    const labels = new Set();
    if (group.type === "BUG" || group.type === "VULNERABILITY") {
        labels.add("bug");
    } else {
        labels.add("chore");
    }
    if (group.type === "VULNERABILITY" || group.type === "SECURITY_HOTSPOT") {
        labels.add("security");
    }
    const language = languageOf(group.rule);
    if (language === "csharpsquid" || language === "roslyn") {
        labels.add(".NET");
    } else if (["typescript", "javascript", "css", "Web", "vue"].includes(language)) {
        labels.add("javascript");
    } else if (language === "githubactions") {
        labels.add("github_actions");
    } else if (language === "docker") {
        labels.add("docker");
    }
    return [...labels];
}

export function issueTypeFor(group) {
    return group.type === "BUG" || group.type === "VULNERABILITY" ? "Bug" : "Task";
}

export function titleFor(group, ruleName) {
    return `SonarQube: ${ruleName ?? group.rule}`;
}

export function bodyFor(group, { ruleName, sonarUrl, projectKey, organization, repository, revision }) {
    const rule = encodeURIComponent(group.rule);
    const project = encodeURIComponent(projectKey);
    const findingsUrl = group.type === "SECURITY_HOTSPOT"
        ? `${sonarUrl}/project/security_hotspots?id=${project}`
        : `${sonarUrl}/project/issues?id=${project}&rules=${rule}&issueStatuses=OPEN%2CCONFIRMED`;
    const ruleUrl = `${sonarUrl}/organizations/${encodeURIComponent(organization)}/rules?open=${rule}&rule_key=${rule}`;
    const count = group.findings.length;
    const lines = [
        marker(group.rule),
        `SonarQube Cloud reports **${count} open finding${count === 1 ? "" : "s"}** of rule [${escape(ruleName ?? group.rule)}](${ruleUrl}) (\`${group.rule}\`) on \`main\`.`,
        "",
        `- Type: ${typeNames[group.type]}`,
        `- Highest severity: ${group.severity.toLowerCase()}`,
        `- Analysed commit: ${revision}`,
        `- [Open these findings in SonarQube Cloud](${findingsUrl})`,
        "",
        "## Findings",
        "",
    ];
    for (const finding of group.findings.slice(0, maxListedFindings)) {
        const location = finding.line ? `${finding.path}#L${finding.line}` : finding.path;
        const label = finding.line ? `${finding.path}:${finding.line}` : finding.path;
        lines.push(`- [\`${label}\`](https://github.com/${repository}/blob/${revision}/${location}): ${escape(finding.message)}`);
    }
    if (count > maxListedFindings) {
        lines.push(`- …and ${count - maxListedFindings} more in SonarQube Cloud.`);
    }
    lines.push(
        "",
        "This issue is updated by the weekly SonarQube workflow. It closes itself when the rule has no open findings on `main`. "
        + "To stop reporting findings that are intended, mark them as accepted or false positive in SonarQube Cloud instead of closing this issue.",
    );
    return lines.join("\n");
}

// Decides what to do with each rule: create, update, reopen or close an issue, or leave it alone.
export function plan({ groups, existing, maxNewIssues }) {
    const byRule = new Map();
    for (const issue of existing) {
        const rule = ruleFromBody(issue.body);
        // Prefer an open issue when a rule somehow has several.
        if (rule && (!byRule.has(rule) || issue.state === "open")) {
            byRule.set(rule, issue);
        }
    }

    const actions = [];
    let created = 0;
    const reported = new Set();
    for (const group of groups) {
        reported.add(group.rule);
        const issue = byRule.get(group.rule);
        if (!issue) {
            if (created < maxNewIssues) {
                actions.push({ kind: "create", group });
                created++;
            } else {
                actions.push({ kind: "defer", group });
            }
        } else if (issue.state === "open") {
            actions.push({ kind: "update", group, issue });
        } else if (issue.state_reason === "not_planned") {
            actions.push({ kind: "skip", group, issue });
        } else {
            actions.push({ kind: "reopen", group, issue });
        }
    }
    for (const [rule, issue] of byRule) {
        if (!reported.has(rule) && issue.state === "open") {
            actions.push({ kind: "close", rule, issue });
        }
    }
    return actions;
}

async function request(fetchImpl, url, { token, method = "GET", body, headers = {} }) {
    const response = await fetchImpl(url, {
        method,
        headers: { Authorization: `Bearer ${token}`, Accept: "application/json", ...headers, ...(body ? { "Content-Type": "application/json" } : {}) },
        body: body ? JSON.stringify(body) : undefined,
    });
    if (!response.ok) {
        const text = await response.text();
        const error = new Error(`${method} ${url} failed with ${response.status}: ${text.slice(0, 500)}`);
        error.status = response.status;
        throw error;
    }
    return response.status === 204 ? null : response.json();
}

export function createSonarClient({ fetchImpl = fetch, sonarUrl, token, organization, projectKey }) {
    const apiUrl = sonarUrl.replace(/\/$/, "");
    const get = (path, params) => request(fetchImpl, `${apiUrl}${path}?${new URLSearchParams(params)}`, { token });

    async function pages(path, params, key) {
        const items = [];
        const extra = [];
        // SonarQube returns at most 10,000 results per search.
        for (let page = 1; page <= 20; page++) {
            const result = await get(path, { ...params, ps: "500", p: String(page) });
            items.push(...(result[key] ?? []));
            extra.push(...(result.rules ?? []));
            const total = result.paging?.total ?? result.total ?? 0;
            if (page * 500 >= total) {
                break;
            }
        }
        return { items, rules: extra };
    }

    return {
        async waitForTask(taskId, { attempts = 60, delayMs = 5000, sleep = ms => new Promise(resolve => setTimeout(resolve, ms)) } = {}) {
            for (let attempt = 0; attempt < attempts; attempt++) {
                const { task } = await get("/api/ce/task", { id: taskId });
                if (task.status === "SUCCESS") {
                    return task;
                }
                if (task.status === "FAILED" || task.status === "CANCELED") {
                    throw new Error(`SonarQube Cloud could not process the analysis: ${task.status} ${task.errorMessage ?? ""}`.trim());
                }
                await sleep(delayMs);
            }
            throw new Error("SonarQube Cloud did not finish processing the analysis in time.");
        },
        // The name of the project's main branch. Only the main branch keeps every open finding; another branch
        // (such as a "main" that is not the project's main branch) shows only the findings in its changed code.
        async mainBranch() {
            const { branches } = await get("/api/project_branches/list", { project: projectKey });
            return branches?.find(branch => branch.isMain)?.name ?? null;
        },
        async issues() {
            return pages("/api/issues/search", { componentKeys: projectKey, organization, resolved: "false", additionalFields: "rules" }, "issues");
        },
        async hotspots() {
            return (await pages("/api/hotspots/search", { projectKey, status: "TO_REVIEW" }, "hotspots")).items;
        },
        async ruleName(rule) {
            try {
                return (await get("/api/rules/show", { key: rule, organization })).rule?.name ?? null;
            } catch {
                return null;
            }
        },
    };
}

export function createGitHubClient({ fetchImpl = fetch, apiUrl = "https://api.github.com", token, repository }) {
    const call = (path, options = {}) => request(fetchImpl, `${apiUrl}/repos/${repository}${path}`, {
        token,
        headers: { Accept: "application/vnd.github+json", "X-GitHub-Api-Version": "2022-11-28" },
        ...options,
    });

    return {
        async labels() {
            const names = [];
            for (let page = 1; ; page++) {
                const batch = await call(`/labels?per_page=100&page=${page}`);
                names.push(...batch.map(label => label.name));
                if (batch.length < 100) {
                    return names;
                }
            }
        },
        // Open and closed issues that carry a rule marker. Pull requests are left out.
        async issues() {
            const issues = [];
            for (let page = 1; ; page++) {
                const batch = await call(`/issues?state=all&per_page=100&page=${page}`);
                issues.push(...batch.filter(issue => !issue.pull_request && ruleFromBody(issue.body)));
                if (batch.length < 100) {
                    return issues;
                }
            }
        },
        async create({ title, body, labels, type }) {
            try {
                return await call("/issues", { method: "POST", body: { title, body, labels, type } });
            } catch (error) {
                // Issue types are an organization setting; create the issue without one when it is not available.
                if (error.status === 422 && type) {
                    return call("/issues", { method: "POST", body: { title, body, labels } });
                }
                throw error;
            }
        },
        update(number, fields) {
            return call(`/issues/${number}`, { method: "PATCH", body: fields });
        },
        comment(number, body) {
            return call(`/issues/${number}/comments`, { method: "POST", body: { body } });
        },
    };
}

// Fails when the analysis did not land on the project's main branch. Issues and hotspots are read from the main
// branch, so they would otherwise describe an older analysis and keep findings open that were fixed long ago.
export async function ensureMainBranch(sonar, task) {
    if (!task?.branch) {
        return;
    }
    const mainBranch = await sonar.mainBranch();
    if (mainBranch && task.branch !== mainBranch) {
        throw new Error(
            `The analysis went to the branch "${task.branch}", but the main branch of the SonarQube Cloud project is "${mainBranch}", `
            + `so its findings are not from this analysis. In SonarQube Cloud, delete the branch "${task.branch}" and rename the main branch `
            + `to "${task.branch}" under Administration → Branches and Pull Requests, then run this workflow again. See docs/sonarqube.md.`);
    }
}

export async function sync({ sonar, github, settings, log = console.log }) {
    const { items: issues, rules } = await sonar.issues();
    const hotspots = await sonar.hotspots();
    const groups = groupByRule(toFindings({ issues, hotspots, projectKey: settings.projectKey, minSeverity: settings.minSeverity }));
    const names = new Map(rules.map(rule => [rule.key, rule.name]));
    for (const group of groups) {
        if (!names.has(group.rule)) {
            names.set(group.rule, await sonar.ruleName(group.rule));
        }
    }

    const available = new Set(await github.labels());
    const existing = await github.issues();
    const actions = plan({ groups, existing, maxNewIssues: settings.maxNewIssues });
    const content = group => ({
        title: titleFor(group, names.get(group.rule)),
        body: bodyFor(group, { ...settings, ruleName: names.get(group.rule) }),
    });

    const summary = [];
    for (const action of actions) {
        const rule = action.group?.rule ?? action.rule;
        const count = action.group?.findings.length ?? 0;
        const line = `${action.kind} ${rule}${action.issue ? ` (#${action.issue.number})` : ""}${count ? `: ${count} finding${count === 1 ? "" : "s"}` : ""}`;
        summary.push({ ...action, line });
        log(line);
        if (settings.dryRun) {
            continue;
        }
        if (action.kind === "create") {
            const labels = labelsFor(action.group).filter(label => available.has(label));
            const created = await github.create({ ...content(action.group), labels, type: issueTypeFor(action.group) });
            summary.at(-1).line += ` → #${created.number}`;
        } else if (action.kind === "update") {
            const fields = content(action.group);
            if (fields.title !== action.issue.title || fields.body !== action.issue.body) {
                await github.update(action.issue.number, fields);
            }
        } else if (action.kind === "reopen") {
            await github.update(action.issue.number, { ...content(action.group), state: "open" });
            await github.comment(action.issue.number, `SonarQube Cloud reports open findings of this rule again in the analysis of ${settings.revision}, so this issue is reopened.`);
        } else if (action.kind === "close") {
            await github.comment(action.issue.number, `SonarQube Cloud reports no open findings of this rule in the analysis of ${settings.revision}, so this issue is closed.`);
            await github.update(action.issue.number, { state: "closed", state_reason: "completed" });
        }
    }
    return summary;
}

function setting(name, fallback) {
    const value = process.env[name];
    if (value === undefined || value === "") {
        if (fallback === undefined) {
            throw new Error(`Environment variable ${name} is required.`);
        }
        return fallback;
    }
    return value;
}

async function main() {
    const minSeverity = setting("SONAR_ISSUES_MIN_SEVERITY", "MEDIUM").toUpperCase();
    if (!severities.includes(minSeverity)) {
        throw new Error(`SONAR_ISSUES_MIN_SEVERITY must be one of ${severities.join(", ")}.`);
    }
    const maxNewIssues = Number.parseInt(setting("SONAR_ISSUES_MAX_NEW", "25"), 10);
    if (!Number.isInteger(maxNewIssues) || maxNewIssues < 0) {
        throw new Error("SONAR_ISSUES_MAX_NEW must be a whole number of zero or more.");
    }
    const settings = {
        sonarUrl: setting("SONAR_HOST_URL", "https://sonarcloud.io").replace(/\/$/, ""),
        organization: setting("SONAR_ORGANIZATION"),
        projectKey: setting("SONAR_PROJECT_KEY"),
        repository: setting("GITHUB_REPOSITORY"),
        revision: setting("SONAR_REVISION"),
        minSeverity,
        maxNewIssues,
        dryRun: setting("SONAR_ISSUES_DRY_RUN", "false") === "true",
    };
    const sonar = createSonarClient({ ...settings, token: setting("SONAR_TOKEN") });
    const github = createGitHubClient({ token: setting("GITHUB_TOKEN"), repository: settings.repository, apiUrl: setting("GITHUB_API_URL", "https://api.github.com") });

    const taskId = process.env.SONAR_CE_TASK_ID;
    if (taskId) {
        await ensureMainBranch(sonar, await sonar.waitForTask(taskId));
    }
    const summary = await sync({ sonar, github, settings });

    const counts = Object.groupBy(summary, action => action.kind);
    const report = [
        `## SonarQube findings${settings.dryRun ? " (dry run, no issues changed)" : ""}`,
        "",
        `Analysed commit ${settings.revision}, findings of severity ${minSeverity.toLowerCase()} and higher, grouped per rule.`,
        "",
        ...["create", "update", "reopen", "close", "skip", "defer"].map(kind => `- ${kind}: ${counts[kind]?.length ?? 0}`),
        "",
        ...(counts.defer?.length ? [`${counts.defer.length} rule(s) wait for a later run because at most ${maxNewIssues} issues are opened per run.`, ""] : []),
        ...(counts.skip?.length ? ["Rules whose issue was closed as not planned still have open findings; dismiss them in SonarQube Cloud.", ""] : []),
        "<details><summary>All rules</summary>",
        "",
        ...summary.map(action => `- ${action.line}`),
        "",
        "</details>",
    ].join("\n");
    if (process.env.GITHUB_STEP_SUMMARY) {
        const { appendFile } = await import("node:fs/promises");
        await appendFile(process.env.GITHUB_STEP_SUMMARY, report + "\n");
    } else {
        console.log(report);
    }
}

if (import.meta.main) {
    main().catch(error => {
        console.error(error.message);
        process.exit(1);
    });
}

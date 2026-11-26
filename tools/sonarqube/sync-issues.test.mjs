import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
    bodyFor,
    createSonarClient,
    ensureMainBranch,
    groupByRule,
    labelsFor,
    marker,
    plan,
    ruleFromBody,
    severityOf,
    sync,
    toFindings,
} from "./sync-issues.mjs";

const projectKey = "Atelix_Starter";
const settings = {
    sonarUrl: "https://sonarcloud.io",
    organization: "atelix",
    projectKey,
    repository: "Atelix/starter",
    revision: "abc123",
    minSeverity: "MEDIUM",
    maxNewIssues: 25,
    dryRun: false,
};

function issue(rule, overrides = {}) {
    return {
        rule,
        type: "CODE_SMELL",
        severity: "MAJOR",
        component: `${projectKey}:Starter/Logic/Example.cs`,
        line: 10,
        message: "Fix <this> for @someone",
        ...overrides,
    };
}

describe("severityOf", () => {
    it("uses the highest impact severity", () => {
        assert.equal(severityOf({ severity: "MINOR", impacts: [{ severity: "LOW" }, { severity: "HIGH" }] }), "HIGH");
    });

    it("falls back to the legacy severity", () => {
        assert.equal(severityOf({ severity: "CRITICAL" }), "HIGH");
        assert.equal(severityOf({ severity: "MINOR" }), "LOW");
    });
});

describe("toFindings and groupByRule", () => {
    it("drops findings below the minimum severity and groups the rest per rule", () => {
        const findings = toFindings({
            issues: [
                issue("csharpsquid:S1", { line: 20 }),
                issue("csharpsquid:S1", { line: 5, severity: "CRITICAL" }),
                issue("csharpsquid:S2", { severity: "MINOR" }),
                issue("typescript:S3", { type: "BUG", component: `${projectKey}:Starter.PWA/src/main.ts` }),
            ],
            hotspots: [{ ruleKey: "githubactions:S4", vulnerabilityProbability: "LOW", component: `${projectKey}:.github/workflows/a.yml`, line: 3 }],
            projectKey,
            minSeverity: "MEDIUM",
        });
        const groups = groupByRule(findings);

        assert.deepEqual(groups.map(group => group.rule), ["csharpsquid:S1", "typescript:S3"]);
        assert.equal(groups[0].severity, "HIGH");
        assert.deepEqual(groups[0].findings.map(finding => finding.line), [5, 20]);
        assert.equal(groups[0].findings[0].path, "Starter/Logic/Example.cs");
    });

    it("keeps security hotspots at or above the minimum severity", () => {
        const findings = toFindings({
            issues: [],
            hotspots: [{ ruleKey: "githubactions:S4", vulnerabilityProbability: "MEDIUM", component: `${projectKey}:.github/workflows/a.yml`, line: 3, message: "Pin it" }],
            projectKey,
            minSeverity: "MEDIUM",
        });
        assert.deepEqual(findings, [{ rule: "githubactions:S4", type: "SECURITY_HOTSPOT", severity: "MEDIUM", path: ".github/workflows/a.yml", line: 3, message: "Pin it" }]);
    });
});

describe("labelsFor", () => {
    it("maps the finding type and language to existing labels", () => {
        assert.deepEqual(labelsFor({ rule: "csharpsquid:S1", type: "VULNERABILITY" }), ["bug", "security", ".NET"]);
        assert.deepEqual(labelsFor({ rule: "typescript:S1", type: "CODE_SMELL" }), ["chore", "javascript"]);
        assert.deepEqual(labelsFor({ rule: "githubactions:S1", type: "SECURITY_HOTSPOT" }), ["chore", "security", "github_actions"]);
    });
});

describe("bodyFor", () => {
    it("carries the marker, links each finding to the analysed commit and escapes messages", () => {
        const [group] = groupByRule(toFindings({ issues: [issue("csharpsquid:S1")], hotspots: [], projectKey, minSeverity: "MEDIUM" }));
        const body = bodyFor(group, { ...settings, ruleName: "Example rule" });

        assert.equal(ruleFromBody(body), "csharpsquid:S1");
        assert.match(body, /\[`Starter\/Logic\/Example\.cs:10`\]\(https:\/\/github\.com\/Atelix\/starter\/blob\/abc123\/Starter\/Logic\/Example\.cs#L10\)/);
        assert.match(body, /Fix &lt;this&gt; for &#64;someone/);
        assert.doesNotMatch(body, /@someone/);
    });

    it("lists at most 50 findings", () => {
        const issues = Array.from({ length: 60 }, (_, index) => issue("csharpsquid:S1", { line: index + 1 }));
        const [group] = groupByRule(toFindings({ issues, hotspots: [], projectKey, minSeverity: "MEDIUM" }));
        const body = bodyFor(group, settings);

        assert.equal(body.split("\n").filter(line => line.startsWith("- [`")).length, 50);
        assert.match(body, /and 10 more in SonarQube Cloud/);
    });
});

describe("plan", () => {
    const group = rule => ({ rule, type: "CODE_SMELL", severity: "MEDIUM", findings: [{}] });
    const existing = (number, rule, state, stateReason = null) => ({ number, body: `${marker(rule)}\ntext`, state, state_reason: stateReason });

    it("creates, updates, reopens, skips and closes issues per rule", () => {
        const actions = plan({
            groups: [group("r:new"), group("r:open"), group("r:fixed-before"), group("r:not-planned")],
            existing: [
                existing(1, "r:open", "open"),
                existing(2, "r:fixed-before", "closed", "completed"),
                existing(3, "r:not-planned", "closed", "not_planned"),
                existing(4, "r:gone", "open"),
                existing(5, "r:gone-closed", "closed", "completed"),
                { number: 6, body: "No marker", state: "open" },
            ],
            maxNewIssues: 10,
        });

        assert.deepEqual(actions.map(action => [action.kind, action.group?.rule ?? action.rule, action.issue?.number]), [
            ["create", "r:new", undefined],
            ["update", "r:open", 1],
            ["reopen", "r:fixed-before", 2],
            ["skip", "r:not-planned", 3],
            ["close", "r:gone", 4],
        ]);
    });

    it("prefers the open issue when a rule has several", () => {
        const actions = plan({ groups: [group("r:a")], existing: [existing(1, "r:a", "open"), existing(2, "r:a", "closed", "completed")], maxNewIssues: 10 });
        assert.deepEqual(actions.map(action => [action.kind, action.issue.number]), [["update", 1]]);
    });

    it("defers new issues beyond the limit", () => {
        const actions = plan({ groups: [group("r:a"), group("r:b"), group("r:c")], existing: [], maxNewIssues: 2 });
        assert.deepEqual(actions.map(action => action.kind), ["create", "create", "defer"]);
    });
});

describe("createSonarClient", () => {
    function fakeFetch(responses) {
        const calls = [];
        const impl = async url => {
            calls.push(url);
            const path = new URL(url).pathname;
            const next = responses[path].shift();
            return { ok: true, status: 200, json: async () => next, text: async () => "" };
        };
        return { impl, calls };
    }

    it("pages through issues and keeps the rule names", async () => {
        const { impl, calls } = fakeFetch({
            "/api/issues/search": [
                { issues: Array.from({ length: 500 }, () => issue("r:a")), rules: [{ key: "r:a", name: "Rule A" }], paging: { total: 501 } },
                { issues: [issue("r:b")], rules: [{ key: "r:b", name: "Rule B" }], paging: { total: 501 } },
            ],
        });
        const sonar = createSonarClient({ fetchImpl: impl, sonarUrl: "https://sonarcloud.io", token: "t", organization: "atelix", projectKey });
        const { items, rules } = await sonar.issues();

        assert.equal(items.length, 501);
        assert.deepEqual(rules.map(rule => rule.name), ["Rule A", "Rule B"]);
        assert.equal(calls.length, 2);
        assert.match(calls[0], /componentKeys=Atelix_Starter/);
        assert.match(calls[0], /resolved=false/);
    });

    it("waits until the analysis is processed and fails on a failed task", async () => {
        const { impl } = fakeFetch({ "/api/ce/task": [{ task: { status: "IN_PROGRESS" } }, { task: { status: "SUCCESS" } }, { task: { status: "FAILED", errorMessage: "boom" } }] });
        const sonar = createSonarClient({ fetchImpl: impl, sonarUrl: "https://sonarcloud.io", token: "t", organization: "atelix", projectKey });
        const sleep = async () => {};

        assert.equal((await sonar.waitForTask("1", { sleep })).status, "SUCCESS");
        await assert.rejects(sonar.waitForTask("2", { sleep }), /FAILED boom/);
    });

    it("finds the project's main branch", async () => {
        const { impl, calls } = fakeFetch({
            "/api/project_branches/list": [{ branches: [{ name: "main", isMain: false, type: "SHORT" }, { name: "master", isMain: true, type: "LONG" }] }],
        });
        const sonar = createSonarClient({ fetchImpl: impl, sonarUrl: "https://sonarcloud.io", token: "t", organization: "atelix", projectKey });

        assert.equal(await sonar.mainBranch(), "master");
        assert.match(calls[0], /project=Atelix_Starter/);
    });
});

describe("ensureMainBranch", () => {
    const sonarWithMain = name => ({ mainBranch: async () => name });

    it("accepts an analysis of the main branch", async () => {
        await assert.doesNotReject(ensureMainBranch(sonarWithMain("main"), { status: "SUCCESS", branch: "main" }));
    });

    it("rejects an analysis that went to another branch, whose findings would be stale", async () => {
        await assert.rejects(ensureMainBranch(sonarWithMain("master"), { status: "SUCCESS", branch: "main" }), /main branch of the SonarQube Cloud project is "master"/);
    });

    it("skips the check when the task names no branch", async () => {
        await assert.doesNotReject(ensureMainBranch({ mainBranch: async () => assert.fail("not asked") }, { status: "SUCCESS" }));
    });
});

describe("sync", () => {
    function fakeGitHub(existing, labels = ["bug", "chore", "security", ".NET", "javascript"]) {
        const calls = [];
        return {
            calls,
            labels: async () => labels,
            issues: async () => {
                calls.push(["issues"]);
                return existing;
            },
            create: async fields => {
                calls.push(["create", fields]);
                return { number: 100 };
            },
            update: async (number, fields) => calls.push(["update", number, fields]),
            comment: async (number, body) => calls.push(["comment", number, body]),
        };
    }

    const sonar = {
        issues: async () => ({ items: [issue("csharpsquid:S1", { type: "BUG" })], rules: [{ key: "csharpsquid:S1", name: "Null check" }] }),
        hotspots: async () => [],
        ruleName: async () => null,
    };

    it("creates an issue with type, existing labels and the rule name", async () => {
        const github = fakeGitHub([], ["bug", ".NET"]);
        await sync({ sonar, github, settings, log: () => {} });

        const [, fields] = github.calls.find(call => call[0] === "create");
        assert.equal(fields.title, "SonarQube: Null check");
        assert.equal(fields.type, "Bug");
        assert.deepEqual(fields.labels, ["bug", ".NET"]);
    });

    it("does not touch an issue whose content is unchanged, and closes issues of fixed rules", async () => {
        const first = fakeGitHub([]);
        await sync({ sonar, github: first, settings, log: () => {} });
        const [, created] = first.calls.find(call => call[0] === "create");

        const github = fakeGitHub([
            { number: 7, state: "open", ...created },
            { number: 8, state: "open", title: "SonarQube: Old", body: marker("csharpsquid:S9") },
        ]);
        await sync({ sonar, github, settings, log: () => {} });

        assert.deepEqual(github.calls.filter(call => call[0] !== "issues").map(call => call.slice(0, 2)), [["comment", 8], ["update", 8]]);
        assert.deepEqual(github.calls.at(-1)[2], { state: "closed", state_reason: "completed" });
    });

    it("changes nothing in a dry run", async () => {
        const github = fakeGitHub([]);
        const summary = await sync({ sonar, github, settings: { ...settings, dryRun: true }, log: () => {} });

        assert.deepEqual(summary.map(action => action.kind), ["create"]);
        assert.deepEqual(github.calls.map(call => call[0]), ["issues"]);
    });
});

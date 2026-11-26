// Renames the template to your project, once, right after creating a repository from it:
//
//   node tools/init/rename.mjs --name MyProject [--owner @your-user-or-team]
//
// It replaces Starter/starter/STARTER in every tracked file and in file and folder names (projects, namespaces,
// Worker names, image names, the database name), sets the CODEOWNERS owner, regenerates the agent skill adapters and
// re-sorts the C# usings with dotnet format (when the .NET SDK is installed).
// Review the result with `git status` and `git diff`, then run the checks in AGENTS.md.
import { execFileSync } from "node:child_process";
import { readFileSync, renameSync, writeFileSync, existsSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const args = process.argv.slice(2);
const option = (flag) => {
    const index = args.indexOf(flag);
    return index >= 0 ? args[index + 1] : undefined;
};

const name = option("--name");
const owner = option("--owner");
if (!name || !/^[A-Z][A-Za-z0-9]*$/.test(name) || name === "Starter") {
    console.error("Usage: node tools/init/rename.mjs --name MyProject [--owner @user-or-org/team]");
    console.error("--name is PascalCase letters and digits, for example Keeva or ClubPlanner.");
    process.exit(1);
}
if (owner && !/^@[A-Za-z0-9-]+(\/[A-Za-z0-9-]+)?$/.test(owner)) {
    console.error("--owner is a GitHub user or team, for example @octocat or @my-org/maintainers.");
    process.exit(1);
}

/** ClubPlanner → club-planner, for Worker, image and package names. */
const kebab = name.replace(/([a-z0-9])([A-Z])/g, "$1-$2").toLowerCase();
/** ClubPlanner → CLUB_PLANNER, for environment variables. */
const upper = kebab.replace(/-/g, "_").toUpperCase();
/** ClubPlanner → club_planner, for database names and other identifiers without dashes. */
const snake = kebab.replace(/-/g, "_");

// The pinned ECC references are verified by hash and never edited; this script is left as it is.
const skip = (path) => path.startsWith("docs/ai/ecc/skills/") || path === "docs/ai/ecc/LICENSE" || path.startsWith("tools/init/")
    || path.endsWith("pnpm-lock.yaml") || /\.(png|jpe?g|gif|ico|webp|woff2?)$/.test(path);

const files = execFileSync("git", ["ls-files", "-z"], { cwd: root, encoding: "utf8" }).split("\0").filter(Boolean);

let changed = 0;
for (const path of files) {
    if (skip(path) || !existsSync(join(root, path))) continue;
    const before = readFileSync(join(root, path), "utf8");
    let after = before
        .replace(/STARTER_/g, upper + "_")
        .replace(/Starter/g, name)
        .replace(/starter_test_/g, snake + "_test_")
        .replace(/starter-/g, kebab + "-")
        .replace(/\bstarter\b/g, kebab);
    if (owner && path === ".github/CODEOWNERS") after = after.replace(/@NealGeilen/g, owner);
    if (after !== before) {
        writeFileSync(join(root, path), after);
        changed++;
    }
}

// Deepest paths first, so a folder is renamed after the files inside it.
const paths = new Set();
for (const path of files) {
    const parts = path.split("/");
    for (let i = 1; i <= parts.length; i++) paths.add(parts.slice(0, i).join("/"));
}
const renames = [...paths].filter((path) => path.split("/").pop().includes("Starter"))
    .sort((a, b) => b.split("/").length - a.split("/").length);
for (const path of renames) {
    const parts = path.split("/");
    const target = [...parts.slice(0, -1), parts.at(-1).replace(/Starter/g, name)].join("/");
    renameSync(join(root, path), join(root, target));
}

execFileSync(process.execPath, ["tools/ai/sync-skills.mjs", "--write"], { cwd: root, stdio: "inherit" });

// The new name sorts differently among the usings, so let dotnet format put them back in order.
const format = [
    ["format", "style", `${name}.sln`, "--exclude", `${name}/Migrations`],
    ["format", "style", `${name}.WebApi.PostgresTests`],
];
try {
    for (const command of format) execFileSync("dotnet", command, { cwd: root, stdio: "inherit" });
} catch {
    console.warn(`Could not run dotnet format. Run it yourself: dotnet format style ${name}.sln --exclude ${name}/Migrations`);
}
console.log(`Renamed Starter to ${name} (${kebab}) in ${changed} files and ${renames.length} paths.`);
console.log("Next: delete the Item example when you add your first feature, then run the checks in AGENTS.md.");

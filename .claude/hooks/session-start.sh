#!/bin/bash
# Prepares Claude Code on the web sessions so the validation suite in AGENTS.md can run. It installs
# the .NET 10 SDK (from Ubuntu's archive, because the dotnet-install script host is not reachable)
# and the Node version from .node-version, restores the solution, the local dotnet tools and the
# PWA's pnpm packages, and adds Python Playwright for the webapp-testing skill.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

cd "${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel)}"

if ! command -v dotnet >/dev/null 2>&1; then
  apt-get update -qq
  DEBIAN_FRONTEND=noninteractive apt-get install -y -qq dotnet-sdk-10.0
fi

if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    echo 'export DOTNET_NOLOGO=true'
    echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=true'
  } >> "$CLAUDE_ENV_FILE"
fi

# The image ships its own Node; use the version from .node-version like CI and the Dockerfiles do.
node_version="$(tr -d '[:space:]' < .node-version)"
node_dir="$HOME/.local/node-v$node_version-linux-x64"
if [ "$(node --version 2>/dev/null)" != "v$node_version" ]; then
  if [ ! -x "$node_dir/bin/node" ]; then
    mkdir -p "$HOME/.local"
    curl -fsSL "https://nodejs.org/dist/v$node_version/node-v$node_version-linux-x64.tar.xz" | tar -xJ -C "$HOME/.local"
  fi
  export PATH="$node_dir/bin:$PATH"
  if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
    echo "export PATH=\"$node_dir/bin:\$PATH\"" >> "$CLAUDE_ENV_FILE"
  fi
fi

# The PWA pins its pnpm version in package.json (packageManager); corepack provides exactly that one.
corepack enable >/dev/null 2>&1 || npm install -g "$(node -p "require('./Starter.PWA/package.json').packageManager")" >/dev/null

dotnet restore Starter.sln --nologo -v quiet
dotnet tool restore >/dev/null

# The webapp-testing skill drives the preinstalled Chromium (/opt/pw-browsers) from Python.
if ! python3 -c "import playwright" >/dev/null 2>&1; then
  PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1 python3 -m pip install -q playwright
fi

pnpm --dir Starter.PWA install --prefer-offline

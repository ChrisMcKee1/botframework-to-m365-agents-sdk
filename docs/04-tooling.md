# 04 — Tooling

Backing notes: [`../research/07-tooling.md`](../research/07-tooling.md). Citations: [`../research/sources.md`](../research/sources.md).

## What to install on Day 1

| Need | Tool | Required? |
|---|---|---|
| Scaffold a new agent project | **Microsoft 365 Agents Toolkit** (VS workload, VS Code extension, or CLI) | Recommended |
| Run / debug the new agent locally | **Microsoft 365 Agents Playground** (`@microsoft/teams-app-test-tool` npm) | Recommended for `after` |
| Run / debug the legacy bot | **Bot Framework Emulator** | Required for `before` |
| .NET runtime | **.NET 8 SDK** | Required |
| Node runtime (only if JS/TS) | **Node.js 20+** | Conditional |
| Python runtime (only if Python) | **Python 3.10+** (3.11+ recommended) | Conditional |
| CI / automation | Agents Toolkit **CLI** | Recommended for pipelines |

## Microsoft 365 Agents Toolkit

The evolution of Teams Toolkit. One toolchain for everything: scaffold → debug → provision → publish.

### Three formats

| Format | What it is | Best for |
|---|---|---|
| **VS Code extension** | Marketplace ID `TeamsDevApp.ms-teams-vscode-extension` | TS / JS (Python coming) |
| **Visual Studio workload** | Installable workload in the VS installer | .NET |
| **CLI** | Terminal | Pipelines, automation, headless scenarios |

### What it does

- Scaffolds projects from templates (Echo Agent, Empty Agent, Weather Agent — the latter with Foundry / OpenAI pre-wired; older variants still scaffold Semantic Kernel, swap for Microsoft Agent Framework for new work).
- Provisions Azure resources — Azure Bot, App Registration, optional storage.
- Manages SSO authentication scaffolding.
- Publishes to:
  - Microsoft 365 Copilot
  - Microsoft Teams
  - Web / Email / SMS / Slack / Facebook Messenger / 10+ more (via Azure Bot)
  - Microsoft Commercial Store
- Integrates TypeSpec for Copilot (declarative agent surface).
- Ships CI/CD templates for GitHub Actions and Azure DevOps.

### Government tenants — caveat

**Publishing agents via the Microsoft 365 Agents Toolkit is not supported in Microsoft 365 Government tenants.**

Confirm the target tenant is commercial cloud before assuming Toolkit-based publish flows.

## Microsoft 365 Agents Playground

The local test sandbox. Simulates Teams locally, without a tenant or a tunnel.

- npm: [`@microsoft/teams-app-test-tool`](https://www.npmjs.com/package/@microsoft/teams-app-test-tool)
- **No dev tenant required.**
- **No tunnel (ngrok) required.**
- **No bot/app registration required.**
- Supports mock data and custom activity triggers for complex scenarios.

This is what the `after-agents-sdk/` sample uses. Comes integrated with the Agents Toolkit; can also be run standalone with `npx`.

## Bot Framework Emulator (legacy)

- Archived on GitHub alongside the BF SDK.
- Still works against new Agents-SDK projects if you supply App ID + secret — the .NET migration guide references it for diagnosing local 401s.
- Used in this repo by `samples/before-bot-framework/` only.

## NuGet / npm / pip cleanup

### .NET

- Strip deprecated Bot Framework preview / MyGet feeds from `NuGet.config`.
- Pin package versions (consider `Directory.Packages.props`).
- Remove `Newtonsoft.Json` from `csproj` unless other code requires it.

### Node

- Remove `botbuilder*` packages from `package.json`.
- Replace with `@microsoft/agents-*` equivalents (table in [`03-migration-mapping.md`](./03-migration-mapping.md)).
- Bump `engines.node` to `>=20`.

### Python

- Replace `botbuilder-*` in `requirements.txt` with `microsoft-agents-*`.
- The Agents SDK uses `black` (formatting) and `flake8` (linting) as quality gates — add to dev deps.
- Bump runtime requirement to **≥ 3.10**.

## Recommended CI ordering

1. Restore / install dependencies against the new package set.
2. Build.
3. Unit tests.
4. Smoke test against Agents Playground (optional in pipeline).
5. Publish via Agents Toolkit CLI to the target channel (Teams / M365 Copilot / etc.).

## Next

→ [`05-auth-and-azure-resources.md`](./05-auth-and-azure-resources.md)

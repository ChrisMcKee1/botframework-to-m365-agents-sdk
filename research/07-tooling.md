# Tooling — Agents Toolkit, Playground, Emulator

Sources:
- https://learn.microsoft.com/microsoftteams/platform/toolkit/overview-agents-toolkit
- https://learn.microsoft.com/microsoft-365/copilot/extensibility/create-deploy-agents-sdk
- https://learn.microsoft.com/microsoft-365/copilot/extensibility/m365-agents-sdk
- https://www.npmjs.com/package/@microsoft/teams-app-test-tool

Verified 2026-05-14.

## Microsoft 365 Agents Toolkit (the authoring + publishing tool)

Microsoft 365 Agents Toolkit is the **evolution of Teams Toolkit**. It is the one toolchain Microsoft now points pro-code developers to for building agents.

### Three formats

| Format | What it is | Optimized for |
|---|---|---|
| **VS Code extension** | Marketplace extension | TypeScript / JavaScript (Python coming) |
| **Visual Studio workload** | Installable workload | .NET |
| **CLI** | Terminal / CI/CD | Headless scenarios, automation |

VS Code Marketplace: `TeamsDevApp.ms-teams-vscode-extension`

### What it does

- **Scaffolds projects** from templates: Echo Agent, Empty Agent, Weather Agent (Foundry / OpenAI wired in; older variants ship Semantic Kernel — swap for Microsoft Agent Framework for new work).
- **Provisions Azure resources** (Azure Bot, App Registration, optional storage).
- **Manages SSO authentication** scaffolding.
- **Publishes** to:
  - Microsoft 365 Copilot
  - Microsoft Teams
  - 10+ other channels (Web, Email, SMS, …) via Azure Bot
  - Microsoft Commercial Store
- **Integrates** with TypeSpec for Copilot (declarative agent surface).
- **CI/CD** templates for GitHub Actions and Azure DevOps.

### Government-tenant caveat

> Publishing agents via the Microsoft 365 Agents Toolkit isn't supported in Microsoft 365 Government tenants.

— per https://learn.microsoft.com/microsoft-365/copilot/extensibility/m365-agents-sdk

Confirm whether the target tenant is commercial cloud before assuming Toolkit-based publish flows.

## Microsoft 365 Agents Playground (the local test sandbox)

- npm package: [`@microsoft/teams-app-test-tool`](https://www.npmjs.com/package/@microsoft/teams-app-test-tool)
- Simulates the look, feel, and behavior of Microsoft Teams locally.
- **No dev tenant required.**
- **No tunneling (ngrok) required.**
- **No bot/app registration required.**
- Supports mock data and custom activity triggers for complex scenarios.

Comes integrated with the Agents Toolkit; can also be run standalone.

This is the replacement for Bot Framework Emulator in the **after** scenario. The Emulator still works against Agents SDK apps (the .NET migration guide explicitly references it for diagnosing 401s), but Playground is preferred for new work because it removes tunnel/tenant friction.

## Bot Framework Emulator (legacy — for the `before` sample only)

- Archived on GitHub alongside the BF SDK.
- Still runs against new Agents-SDK projects if you supply App ID + secret.
- In the migration guide, Emulator is the documented escape hatch for diagnosing local 401s.

For our project:

- **`samples/before-bot-framework/`** uses Emulator.
- **`samples/after-agents-sdk/`** uses Agents Playground.

## Tooling stack summary for dev day-1

| Need | Tool |
|---|---|
| Scaffold a new agent | Agents Toolkit (VS or VS Code) |
| Run / debug locally | Agents Playground |
| Connect channels (Teams, M365 Copilot, web, email, SMS, …) | Agents Toolkit publish or Azure Bot configuration |
| Test the BF-side reference (`before` sample) | Bot Framework Emulator |
| CI / automation | Agents Toolkit CLI |

## NuGet / npm / pip cleanup checklist

The migration guides each call this out:

- **.NET:** strip BF preview / MyGet feeds from `NuGet.config`. Pin versions (consider `Directory.Packages.props`). Drop `Newtonsoft.Json` unless other code needs it.
- **Node:** remove `botbuilder*` packages from `package.json`. Replace with `@microsoft/agents-*` equivalents.
- **Python:** replace `botbuilder-*` in `requirements.txt`. Add `black` + `flake8` for the SDK's quality standards.

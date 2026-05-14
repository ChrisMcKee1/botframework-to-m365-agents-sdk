# PRD — Bot Framework → Microsoft 365 Agents SDK Migration Reference Project

**Project codename:** `Microsoft-Agents-365-SDK`
**Status:** v1
**Last updated:** 2026-05-14

---

## 1. Background & Why Now

The **Bot Framework SDK and Bot Framework Emulator were archived on GitHub and support tickets are no longer serviced as of December 31, 2025**. Any team running a Bot Framework SDK v4 codebase needs a forward path.

Microsoft's recommended forward path is the **Microsoft 365 Agents SDK** (C#, JavaScript/TypeScript, Python). The Agents SDK is the evolution of Bot Framework — it keeps the conversational primitives BF developers already understand (turns, activities, state, channels, adapters) while removing legacy pieces (Composer/Adaptive Dialogs, LUIS/QnA, BotFrameworkAdapter, ASP.NET WebAPI) and adding first-class support for modern AI orchestration (Azure AI Foundry, **Microsoft Agent Framework** — the successor to Semantic Kernel + AutoGen — LangChain, OpenAI Agents) and additional channels (M365 Copilot, Teams, Web, Email, SMS, Slack, Facebook Messenger, etc.).

This project produces a **side-by-side reference implementation and migration playbook** that any team can fork as the baseline for a real migration.

---

## 2. Goals

1. Provide a **runnable "before vs. after"** that runs locally — same bot scenario, one in Bot Framework SDK v4, one in Microsoft 365 Agents SDK — so the deltas are visible and not theoretical.
2. Produce a **migration playbook** that maps every concept a typical BF v4 codebase has to its Agents SDK replacement (packages, namespaces, types, configuration, auth, hosting, state, dialogs/middleware).
3. Validate the **AI/orchestration story** — show how Azure AI Foundry / Microsoft Agent Framework slots into the same agent so teams see how to retire LUIS/QnA dependencies.
4. Document the **tooling chain** (Microsoft 365 Agents Toolkit for VS / VS Code, Microsoft 365 Agents Playground, Bot Framework Emulator for legacy debug) so a dev team knows what to install on Day 1.
5. Produce a **discovery checklist** that can be worked through in ~30 minutes to scope an actual migration.

### Non-goals
- Not migrating any specific team's production code. This is a reference.
- Not building net-new agent capabilities — the goal is to demonstrate the migration, not extend the scenario.
- Not covering Copilot Studio (low-code) or pure Teams AI Library paths in depth — they are mentioned as alternatives but not the focus. This repo is for teams migrating an existing pro-code Bot Framework solution.

---

## 3. Audience

- **Pro-code dev teams** (.NET / Node / Python) maintaining an existing Bot Framework SDK v4 solution.
- **Solution architects / leadership** who need confidence that the migration path is sound and that Azure resources (Bot registration, App ID, channels) are preserved.
- **Migration leads** who need a reusable planning artifact before sizing work.

---

## 4. Scope of the Reference Project

### 4.1 Repo layout

```
Microsoft-Agents-365-SDK/
├── PRD.md                              ← this file
├── README.md                           ← quickstart + how to run before/after
├── docs/
│   ├── 01-bot-framework-overview.md    ← what Bot Framework is, what's deprecated
│   ├── 02-agents-sdk-overview.md       ← what M365 Agents SDK is, channels, AI-agnostic design
│   ├── 03-migration-mapping.md         ← package + namespace + type rename tables (C#, JS, Python)
│   ├── 04-tooling.md                   ← Agents Toolkit, Agents Playground, Emulator, NuGet/npm/pip feeds
│   ├── 05-auth-and-azure-resources.md  ← what stays (Azure Bot reg, App ID), what changes (TokenValidation, Connections, MSAL)
│   ├── 06-ai-orchestration-options.md  ← Agent Framework, Foundry, Semantic Kernel, LangChain, OpenAI Agents — when to pick which
│   ├── 07-discovery-checklist.md       ← questions to scope a migration
│   ├── 08-migration-playbook.md        ← end-to-end checklist a dev follows
│   └── 09-running-in-teams.md          ← provision once, sideload, swap SDKs to demo the migration
├── samples/
│   ├── before-bot-framework/           ← BF v4 EchoBot + state + simple dialog (C# .NET 8)
│   ├── after-agents-sdk/               ← same scenario, M365 Agents SDK (C# .NET 8)
│   └── side-by-side/                   ← README that diffs the two with annotated screenshots
└── research/                           ← raw notes, links, citations
```

### 4.2 The reference scenario

A small but realistic bot that exercises the things a typical BF v4 bot uses:

- Activity handler with `OnMessageActivityAsync`
- Conversation + user state via `ConversationState` / `UserState` and a property accessor
- One waterfall dialog (greet → collect a value → confirm)
- Teams channel attachment (adaptive card response)
- Local debug via Bot Framework Emulator (before) and Microsoft 365 Agents Playground (after)

This is built once in **Bot Framework SDK v4** and once in **Microsoft 365 Agents SDK**, both targeting **.NET 8**. Same external behavior, same Azure Bot registration, same App ID — different SDK underneath. JavaScript and Python equivalents documented in `docs/03-migration-mapping.md` but not shipped as full samples in v1.

### 4.3 Key migration deltas the project must demonstrate

| Concern | Bot Framework SDK v4 | Microsoft 365 Agents SDK |
|---|---|---|
| .NET target | .NET 6 typical | .NET 8 (or .NET 6 minimum) |
| Packages (.NET) | `Microsoft.Bot.Builder`, `Microsoft.Bot.Builder.Integration.AspNet.Core`, `Microsoft.Bot.Schema`, `Microsoft.Bot.Connector` | `Microsoft.Agents.Builder`, `Microsoft.Agents.Core`, `Microsoft.Agents.Hosting.AspNetCore`, `Microsoft.Agents.Authentication.Msal`, `Microsoft.Agents.Storage.*`, `Microsoft.Agents.Builder.Teams` / `Teams.Compat` |
| Packages (Python) | `botbuilder-core`, `botbuilder-schema`, `botbuilder-azure`, `botbuilder-integration-aiohttp` | `microsoft-agents-hosting-core`, `microsoft-agents-activity`, `microsoft-agents-storage-blob` / `-cosmos`, `microsoft-agents-hosting-aiohttp`, `microsoft-agents-authentication-msal`, `microsoft-agents-hosting-teams` |
| Packages (JS) | `botbuilder` | `@microsoft/agents-hosting` |
| Adapter | `BotFrameworkAdapter` / `CloudAdapter` | `CloudAdapter` only; HTTP auth handed to ASP.NET / aiohttp |
| Hosting (.NET) | Startup + controller pattern | Minimal API + `builder.AddAgent<T>()` + `AddAgentAspNetAuthentication()` + `MapPost("/api/messages").RequireAuthorization()` |
| Auth | JWT validation inside SDK; `MicrosoftAppId` / `Password` / `TenantId` app settings | ASP.NET / framework owns JWT; new `TokenValidation` + `Connections` config blocks; MSAL via `Microsoft.Agents.Authentication.Msal` |
| State / storage | `IStorage` + `ConversationState` + `UserState` | Same shapes, namespace under `Microsoft.Agents.Storage.*`; fully-qualify when `IStorage` / `IMiddleware` are ambiguous |
| Turn services | `TurnContext.TurnState` | `TurnContext.Services` (`.Services.Get<T>()`) |
| JSON | `JObject` / `JToken` (Newtonsoft) | `JsonDocument` / `JsonElement` (`System.Text.Json`) |
| Dialogs | `Microsoft.Bot.Builder.Dialogs` | Available under `Microsoft.Agents.Builder.Dialogs` for compatibility — bridge during migration, plan refactor |
| Adaptive Dialogs / Composer / LG / Adaptive Expressions | Supported | **Not supported** — must be replaced |
| LUIS / QnA Maker | Supported (retired services) | **Not supported** — replace with Azure OpenAI + retrieval (Foundry / Microsoft Agent Framework) |
| Telemetry | App Insights bot telemetry helpers | Standard cloud-native observability (OpenTelemetry-friendly) |
| Channels | Teams, Web Chat, Slack, etc. via Azure Bot Service | Same Azure Bot registration + adds first-class **M365 Copilot** as a channel |
| Local debug | Bot Framework Emulator | Microsoft 365 Agents Playground (no tunnel/ngrok, no dev tenant required); Emulator still works |
| Azure resources | Azure Bot, App Service / Functions | **Unchanged** — same Azure Bot registration, same App ID/secret |

### 4.4 Tooling we will install / show

- **Microsoft 365 Agents Toolkit** for Visual Studio and VS Code — scaffolding, templates, multi-channel publish (M365 Copilot, Teams, Web, Email, SMS, +10 more)
- **Microsoft 365 Agents Playground** (`@microsoft/teams-app-test-tool` on npm) — local sandbox, no dev tenant or ngrok required
- **Bot Framework Emulator** (legacy, for the "before" sample only)
- NuGet / npm / pip feed cleanup — remove deprecated Bot Framework preview feeds

### 4.5 AI / orchestration variant (optional second "after" sample, stretch goal)

A second `after-agents-sdk-with-foundry/` sample that wires Azure AI Foundry + Microsoft Agent Framework into the same agent so teams see what replacing LUIS/QnA looks like in practice. Build only if the team using this repo currently depends on LUIS or QnA — confirm before scaffolding.

---

## 5. Planning deliverables (what a team using this reference should produce)

1. Link to this repo as the starting reference.
2. The completed **discovery checklist** (`docs/07-discovery-checklist.md`) filled out for the team's bot — drives a sized migration plan.
3. Agreement on **scope**: lift-and-shift only, or lift-and-shift + retire LUIS/QnA + add Foundry orchestration.
4. List of **Azure resources to keep** (Bot registration, App ID) and **app settings to add/remove** (TokenValidation, Connections; remove `MicrosoftAppType`/`MicrosoftAppId`/`MicrosoftAppPassword`/`MicrosoftAppTenantId` after validation).
5. A go-forward checklist (the migration playbook) the dev team owns.

---

## 6. Discovery checklist (questions to scope a migration)

These live in `docs/07-discovery-checklist.md` and should be worked through to size the migration:

1. **Language and runtime?** C# (.NET 6/8), Node.js, or Python? Java is retired and not on the Agents SDK path.
2. **Hosting?** App Service, Functions, AKS, on-prem? (Affects how `AddAgentAspNetAuthentication` and the Connections block are wired.)
3. **Channels in use?** Teams only, or Teams + Web Chat + others? Are you interested in surfacing in M365 Copilot post-migration?
4. **Composer / Adaptive Dialogs / LG / Adaptive Expressions?** If yes — these don't migrate. Need to refactor or rebuild those flows.
5. **LUIS / QnA Maker?** If yes — retired. Plan replacement with Azure OpenAI + retrieval (Foundry, Microsoft Agent Framework).
6. **Dialogs?** Waterfall? Component? Custom? (Dialogs are still available in Agents SDK for compatibility — good bridge.)
7. **Custom middleware?** Will continue to work via DI; or refactor into turn lifecycle hooks.
8. **State / storage backend?** Memory, Blob, Cosmos? (Direct package swap to `Microsoft.Agents.Storage.*`.)
9. **Auth model?** Single-tenant, multi-tenant, user-assigned MI, federated? Drives MSAL `Connections` configuration.
10. **App Insights / telemetry?** Modernize to standard observability.
11. **CI/CD?** Any pipeline-side dependencies on Bot Framework templates / NuGet preview feeds to scrub?
12. **Test surface?** Existing unit tests, Emulator-driven smoke tests — migrate to Agents Playground?
13. **Timeline pressure?** Bot Framework support tickets ended Dec 31, 2025 — is there a regulatory or business deadline?

---

## 7. Acceptance criteria

- [ ] Both `before-bot-framework/` and `after-agents-sdk/` build clean and run locally against the Emulator and Agents Playground respectively.
- [ ] `docs/03-migration-mapping.md` covers all package, namespace, type, config, hosting, and auth changes for at minimum C# (Node + Python tabled).
- [ ] `docs/07-discovery-checklist.md` is concrete enough to work through in ~30 minutes and produce a sized migration backlog.
- [ ] `docs/08-migration-playbook.md` is the **migration checklist** straight from MS Learn (analyze → upgrade target → replace packages → update namespaces → rewrite `Program.cs` → middleware → build/test → deploy/monitor) with annotations.
- [ ] README has a "5-minute tour" so a dev can clone, open both samples in VS / VS Code, and see the diff.
- [ ] All claims in the docs are cited back to MS Learn (citations live in `research/`).

---

## 8. Risks & open questions (resolve before sizing a migration)

Resolve these before sizing the migration:

- **Your bot's actual complexity is unknown.** This reference is based on a *typical* BF v4 bot; specific gaps may differ. Mitigation: complete the discovery checklist before sizing.
- **LUIS/QnA dependency.** If your bot is on either, lift-and-shift alone won't get your team to a runnable agent — you need an AI replacement story. Confirm before sizing.
- **Government tenant.** Publishing agents via the M365 Agents Toolkit is **not supported** in Microsoft 365 Government tenants. Confirm the target tenant is commercial cloud.
- **Composer artifacts.** Bot Framework Composer flows cannot be carried forward. Need rebuild plan.
- **Sample scope creep.** Keep `samples/` to one scenario per language. JS and Python in v1 are doc-only; build them on demand.

---

## 9. Sources (all MS Learn, verified 2026-05-14)

- *What is the Microsoft 365 Agents SDK* — https://learn.microsoft.com/microsoft-365/agents-sdk/agents-sdk-overview
- *Bot Framework SDK to Agents SDK migration guidance (overview + unsupported packages)* — https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-guidance
- *Migration guidance for .NET* — https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-dotnet
- *Migration guidance for Python* — https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-python
- *Migration guidance for Node.js* — https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-nodejs
- *Microsoft 365 Agents Toolkit overview* — https://learn.microsoft.com/microsoftteams/platform/toolkit/overview-agents-toolkit
- *Build custom engine agents with M365 Agents SDK* — https://learn.microsoft.com/microsoft-365/copilot/extensibility/m365-agents-sdk
- *Custom engine agents overview (tool comparison: Copilot Studio / Teams AI / Agents SDK / Foundry)* — https://learn.microsoft.com/microsoft-365/copilot/extensibility/overview-custom-engine-agent
- *Bot Framework end of support announcement (Dec 31, 2025)* — https://learn.microsoft.com/azure/bot-service/bot-service-resources-links-help
- *Configure authentication in a .NET agent (MSAL options)* — https://learn.microsoft.com/microsoft-365/agents-sdk/microsoft-authentication-library-configuration-options
- *AspNetExtensions reference (sample to copy into project)* — https://github.com/microsoft/Agents/blob/main/samples/dotnet/quickstart/AspNetExtensions.cs

---

## 10. Next actions

1. Scaffold `samples/before-bot-framework/` and `samples/after-agents-sdk/` (.NET 8, EchoBot + state + one waterfall dialog + Teams adaptive card).
2. Draft `docs/03-migration-mapping.md` and `docs/07-discovery-checklist.md` first — these are what teams use to size a migration.
3. Distribute the discovery checklist to stakeholders before the migration kickoff so they can pre-fill answers.

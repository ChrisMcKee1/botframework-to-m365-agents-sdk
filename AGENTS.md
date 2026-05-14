# AGENTS.md — Microsoft-Agents-365-SDK

> Read [`PRD.md`](./PRD.md) first. It defines scope, goals, non-goals, deltas, and acceptance criteria. This file is short-form orientation for AI coding agents.

## What this repo is

A **reference / migration playbook**, not a production bot. Audience: pro-code dev teams migrating a Bot Framework SDK v4 codebase, plus teams that need a reusable planning artifact before sizing work.

The deliverable is a **runnable "before vs. after"**:
- [`samples/before-bot-framework/`](./samples/before-bot-framework/) — Bot Framework SDK v4, .NET 8
- [`samples/after-agents-sdk/`](./samples/after-agents-sdk/) — Microsoft 365 Agents SDK, .NET 8

Same external bot behavior, same Azure Bot registration + App ID, different SDK underneath. Both must build clean and run locally (Emulator for "before", Microsoft 365 Agents Playground for "after").

Docs in [`docs/`](./docs/) follow the layout in [`PRD.md` § 4.1](./PRD.md#41-repo-layout).

## Ground rules for any change

1. **Cite everything to MS Learn.** All technical claims trace to a link in [`research/sources.md`](./research/sources.md). If you add a claim, add the source. Don't invent APIs, namespaces, or package names — verify against the migration guidance pages.
2. **Link, don't embed.** Cross-reference [`PRD.md`](./PRD.md), [`README.md`](./README.md), and `docs/*.md` instead of duplicating content. Migration deltas live in `docs/03-migration-mapping.md`, not scattered.
3. **Keep `before` and `after` in lockstep.** Same scenario, same activities, same state surface, same adaptive card. If you add a feature to one, add (or explicitly defer) it in the other so the diff stays meaningful.
4. **Stay in scope.** No net-new bot capabilities, no Copilot Studio / Teams AI Library deep-dives, no Java. JS and Python are **doc-only in v1** — do not scaffold sample projects for them unless the user asks.
5. **Stretch goals are gated.** The Foundry / Microsoft Agent Framework "after" variant (PRD § 4.5) is conditional on the team using this repo having LUIS/QnA in their existing bot. Don't build it preemptively.

## Conventions an agent needs to know

- **.NET target: `net8.0`** for both samples. PRD allows .NET 6 minimum but we standardize on 8.
- **Reference scenario** (PRD § 4.2): `OnMessageActivityAsync` + `ConversationState`/`UserState` accessor + one waterfall dialog (greet → collect → confirm) + Teams adaptive card response. Don't expand it.
- **Package mapping is canonical** — use the table in [`PRD.md` § 4.3](./PRD.md#43-key-migration-deltas-the-project-must-demonstrate). When generating code for `after-agents-sdk/`:
  - Use `Microsoft.Agents.Builder`, `Microsoft.Agents.Core`, `Microsoft.Agents.Hosting.AspNetCore`, `Microsoft.Agents.Authentication.Msal`, `Microsoft.Agents.Storage.*`, `Microsoft.Agents.Builder.Teams` / `Teams.Compat`.
  - Adapter: `CloudAdapter` only (never `BotFrameworkAdapter`).
  - Hosting: ASP.NET Core **minimal API** with `builder.AddAgent<T>()` + `AddAgentAspNetAuthentication()` + `MapPost("/api/messages").RequireAuthorization()`. Not Startup-class + controllers.
  - JSON: `System.Text.Json` (`JsonDocument`/`JsonElement`). Not Newtonsoft `JObject`/`JToken`.
  - Turn state lookup: `TurnContext.Services.Get<T>()`, not `TurnContext.TurnState`.
  - Fully-qualify `Microsoft.Agents.Storage.IStorage` and `Microsoft.Agents.Builder.IMiddleware` when the unqualified name is ambiguous.
- **Do not introduce** Adaptive Dialogs, Adaptive Expressions, Bot Framework Composer artifacts, LUIS, QnA Maker, ASP.NET WebAPI, or the App Insights bot-telemetry helpers in `after-agents-sdk/`. These are explicitly unsupported (PRD § 4.3, sources.md).
- **App settings.** New samples use the `TokenValidation` + `Connections` blocks (MSAL). Legacy `MicrosoftAppType` / `MicrosoftAppId` / `MicrosoftAppPassword` / `MicrosoftAppTenantId` are tolerated during migration but should not appear in `after-agents-sdk/` final config.
- **Python notes (for docs only).** Package imports use **underscores** (`microsoft_agents`, not `microsoft.agents`). Env config uses **double-underscore** hierarchical naming.

## Working on docs

Doc files are numbered (`01-…` through `08-…`) per [`PRD.md` § 4.1](./PRD.md#41-repo-layout). When creating one, follow that order and link forward/back so the reading path is `README → PRD → docs/01 → … → docs/08`.

`docs/03-migration-mapping.md` and `docs/07-discovery-checklist.md` are the two docs that drive a real migration — prioritize those if asked to "start the docs".

## Build / test (when samples exist)

Both samples are .NET 8. From a sample folder:

```pwsh
dotnet build
dotnet run
```

There is no solution-wide build yet. Don't add CI until at least one sample compiles.

## Pitfalls

- The Microsoft 365 Agents Toolkit **does not support Microsoft 365 Government tenants** for publishing. Don't write docs that assume otherwise (PRD § 8).
- Bot Framework SDK is **archived, not deleted** — the `before` sample is expected to build. If a NuGet package no longer resolves, note it in `research/sources.md` rather than silently swapping packages.
- The repo currently has empty `samples/*` and `docs/` folders. Don't assume files exist before reading.

## Sources

All citations live in [`research/sources.md`](./research/sources.md). Keep that file authoritative — add new links there before referencing them from docs or code comments.

# AGENTS.md — Microsoft-Agents-365-SDK

> Read [`README.md`](./README.md) first for scope and the 5-minute tour. This file is short-form orientation for AI coding agents working in the repo.

## What this repo is

A **reference / migration playbook**, not a production bot. Audience: pro-code dev teams migrating a Bot Framework SDK v4 codebase, plus teams that need a reusable planning artifact before sizing work.

The deliverable is a **runnable "before vs. after"**:
- [`samples/before-bot-framework/`](./samples/before-bot-framework/) — Bot Framework SDK v4, .NET 8
- [`samples/after-agents-sdk/`](./samples/after-agents-sdk/) — Microsoft 365 Agents SDK, .NET 8

Same external bot behavior, same Azure Bot registration + App ID, different SDK underneath. Both must build clean and run locally (Emulator for "before", Microsoft 365 Agents Playground for "after").

The migration playbook lives in [`docs/`](./docs/) (read `01-…` through `09-…` in order). The canonical rename/re-wire tables live in [`docs/03-migration-mapping.md`](./docs/03-migration-mapping.md).

## Ground rules for any change

1. **Cite everything to MS Learn.** All technical claims trace to a link in [`research/sources.md`](./research/sources.md). If you add a claim, add the source. Don't invent APIs, namespaces, or package names — verify against the migration guidance pages.
2. **Link, don't embed.** Cross-reference [`README.md`](./README.md) and `docs/*.md` instead of duplicating content. Migration deltas live in `docs/03-migration-mapping.md`, not scattered.
3. **Keep `before` and `after` in lockstep.** Same scenario, same activities, same state surface, same adaptive card. If you add a feature to one, add (or explicitly defer) it in the other so the diff stays meaningful.
4. **Stay in scope.** No net-new bot capabilities, no Copilot Studio / Teams AI Library deep-dives, no Java. JS and Python are **doc-only in v1** — do not scaffold sample projects for them unless the user asks.
5. **Stretch goals are gated.** The Foundry / Microsoft Agent Framework "after" variant is conditional on the team using this repo having LUIS/QnA in their existing bot. Don't build it preemptively. See [`docs/06-ai-orchestration-options.md`](./docs/06-ai-orchestration-options.md).

## Conventions an agent needs to know

- **.NET target: `net8.0`** for both samples. .NET 6 is the minimum, but standardize on 8.
- **Reference scenario:** `OnMessageActivityAsync` + `ConversationState`/`UserState` accessor + one waterfall dialog (greet → collect → confirm) + Teams adaptive card response. Don't expand it.
- **Package mapping is canonical** — use the tables in [`docs/03-migration-mapping.md`](./docs/03-migration-mapping.md). When generating code for `after-agents-sdk/`:
  - Use `Microsoft.Agents.Builder`, `Microsoft.Agents.Core`, `Microsoft.Agents.Hosting.AspNetCore`, `Microsoft.Agents.Authentication.Msal`, `Microsoft.Agents.Storage.*`, `Microsoft.Agents.Builder.Teams` / `Teams.Compat`.
  - Adapter: `CloudAdapter` only (never `BotFrameworkAdapter`).
  - Hosting: ASP.NET Core **minimal API** with `builder.AddAgent<T>()` + `AddAgentAspNetAuthentication()` + `MapPost("/api/messages").RequireAuthorization()`. Not Startup-class + controllers.
  - JSON: `System.Text.Json` (`JsonDocument`/`JsonElement`). Not Newtonsoft `JObject`/`JToken`.
  - Turn state lookup: `TurnContext.Services.Get<T>()`, not `TurnContext.TurnState`.
  - Fully-qualify `Microsoft.Agents.Storage.IStorage` and `Microsoft.Agents.Builder.IMiddleware` when the unqualified name is ambiguous.
- **Do not introduce** Adaptive Dialogs, Adaptive Expressions, Bot Framework Composer artifacts, LUIS, QnA Maker, ASP.NET WebAPI, or the App Insights bot-telemetry helpers in `after-agents-sdk/`. These are explicitly unsupported — see [`docs/01-bot-framework-overview.md`](./docs/01-bot-framework-overview.md) § "What goes away" and [`research/03-unsupported-and-deprecated.md`](./research/03-unsupported-and-deprecated.md).
- **App settings.** New samples use the `TokenValidation` + `Connections` blocks (MSAL). Legacy `MicrosoftAppType` / `MicrosoftAppId` / `MicrosoftAppPassword` / `MicrosoftAppTenantId` are tolerated during migration but should not appear in `after-agents-sdk/` final config.
- **Python notes (for docs only).** Package imports use **underscores** (`microsoft_agents`, not `microsoft.agents`). Env config uses **double-underscore** hierarchical naming.

## Working on docs

Doc files are numbered (`01-…` through `09-…`). When creating or editing one, follow that order and link forward/back so the reading path is `README → docs/01 → … → docs/09`.

`docs/03-migration-mapping.md` and `docs/07-discovery-checklist.md` are the two highest-leverage docs for a real migration — prioritize those if asked to "start the docs".

## Build / test

Both samples are .NET 8. From a sample folder:

```pwsh
dotnet build
dotnet run
```

There is no solution-wide build pipeline yet. Don't add CI until both samples have at least been smoke-tested end-to-end against the Emulator / Playground.

## Pitfalls

- The Microsoft 365 Agents Toolkit **does not support Microsoft 365 Government tenants** for publishing. Don't write docs that assume otherwise. See [`docs/04-tooling.md`](./docs/04-tooling.md).
- Bot Framework SDK is **archived, not deleted** — the `before` sample is expected to build. If a NuGet package no longer resolves, note it in `research/sources.md` rather than silently swapping packages.

## Sources

All citations live in [`research/sources.md`](./research/sources.md). Keep that file authoritative — add new links there before referencing them from docs or code comments.

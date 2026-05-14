# 01 — Bot Framework SDK overview & what's deprecated

Backing notes: [`../research/01-bf-end-of-life-and-status.md`](../research/01-bf-end-of-life-and-status.md). Citations: [`../research/sources.md`](../research/sources.md).

## TL;DR

| Question | Answer |
|---|---|
| Is our bot broken? | **No.** Existing workloads keep running. |
| Can we still open support tickets? | **No.** Bot Framework SDK support tickets ended **Dec 31, 2025**. |
| Are we getting new features / patches? | **No.** Repos are archived. |
| What does Microsoft now recommend? | **Microsoft 365 Agents SDK** for pro-code (this project). Teams SDK if Teams-only. Copilot Studio for low-code. |
| Do we lose Azure resources during migration? | **No.** Same Azure Bot, same App ID, same channels, same hosting. SDK swap only. |

## What's archived

The following are archived on GitHub and no longer actively maintained:

- **Bot Framework SDK v4** (`microsoft/botbuilder-dotnet`, `botbuilder-js`, `botbuilder-python`)
- **Bot Framework Emulator** (`microsoft/BotFramework-Emulator`)
- **Bot Framework Composer** (`microsoft/BotFramework-Composer`)
- **Bot Framework CLI** (`bf` command, `microsoft/botframework-cli`)

> Archival is **not deletion**. The packages remain on NuGet / npm / PyPI and existing bots continue to run. There is no Microsoft-imposed runtime cutoff.

## What's retired (Azure services, gone)

These are different — the **service** is shut down, not just the SDK:

- **LUIS (Language Understanding)** — service retired. Replace with Azure OpenAI + retrieval.
- **QnA Maker** — service retired. Replace with Azure AI Search + Azure OpenAI, or knowledge sources on a Foundry agent.

If the existing bot calls either, lift-and-shift alone won't yield a working agent — see [`06-ai-orchestration-options.md`](./06-ai-orchestration-options.md).

## Why this matters

This is **modernization, not a crisis**:

- Your bot still runs.
- The pressure is loss of support, security patches, and feature investment — a **business-risk argument**, not a technical-failure one.
- Migration is **incremental**: the new Agents SDK reuses the same Azure Bot registration, so you can build the new bot alongside the old one and cut over channel-by-channel.

## Three forward paths Microsoft now points to

| Path | Best for | Decision driver |
|---|---|---|
| **Microsoft 365 Agents SDK** | Existing pro-code BF teams that want to keep control over orchestration and model choice | The target for this reference. |
| **Teams SDK (Teams AI Library)** | Teams-only collaborative agents | Use only if the bot is Teams-only; skip if you need multi-channel. |
| **Microsoft Copilot Studio** | Low-code / business-user-authored agents | Use when low-code is acceptable; skip if you need pro-code. |

The decision matrix is in [`02-agents-sdk-overview.md`](./02-agents-sdk-overview.md) and [`06-ai-orchestration-options.md`](./06-ai-orchestration-options.md).

## What goes away in the migration (must replace)

Don't try to bring these forward — they aren't supported in Agents SDK:

- Adaptive Dialogs
- AdaptiveExpressions (packages still publish but unsupported)
- Bot Framework Composer + artifacts
- Language Generation (LG) templates
- LUIS / QnA Maker / Orchestrator
- `BotFrameworkAdapter` (use `CloudAdapter`)
- ASP.NET WebAPI (.NET — use ASP.NET Core minimal APIs)
- Bot Framework CLI (`bf`)
- Generators (Yeoman, etc.)
- Inspection middleware
- Streaming Connections (legacy)
- App Insights bot-telemetry helpers (use standard observability)
- `QueueStorage` (BotBuilder)
- `TemplateManager`
- Deprecated activities (payments, etc.)

Full table with rationale: [`../research/03-unsupported-and-deprecated.md`](../research/03-unsupported-and-deprecated.md).

## What stays

- **Azure Bot registration** — same resource, same App ID, same secret.
- **Channels** — Teams, Web Chat, Slack, etc. stay bound to the bot.
- **Hosting** — App Service / Functions / wherever the bot is deployed today.
- **Conversation primitives** — `ConversationState`, `UserState`, `ActivityHandler`, dialogs, `IStorage`, `IMiddleware` — all have direct equivalents in the new SDK.

## Next

→ [`02-agents-sdk-overview.md`](./02-agents-sdk-overview.md)

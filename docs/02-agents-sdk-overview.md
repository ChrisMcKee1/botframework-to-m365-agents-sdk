# 02 — Microsoft 365 Agents SDK overview

Backing notes: [`../research/02-agents-sdk-overview.md`](../research/02-agents-sdk-overview.md), [`../research/11-activity-protocol-and-agentapplication.md`](../research/11-activity-protocol-and-agentapplication.md). Citations: [`../research/sources.md`](../research/sources.md).

## What it is

The **Microsoft 365 Agents SDK** is the pro-code framework for building conversational AI agents. It's the **plumbing between a user message and your business / AI logic** — channel abstraction, state management, message routing — with no opinion on which AI model or orchestration framework you use.

It is the **direct successor to the Bot Framework SDK**. Same conversational primitives (turns, activities, state, channels, adapters), but:

- Modern hosting (ASP.NET Core minimal APIs, not Startup + controllers)
- Modern auth (MSAL via dedicated `Connections` config block, ASP.NET owns JWT)
- Modern AI orchestration (any framework you want — Microsoft Agent Framework, Foundry, Semantic Kernel, LangChain, OpenAI Agents)
- First-class **Microsoft 365 Copilot** as a channel
- Legacy pieces removed (Composer / Adaptive Dialogs / LUIS / QnA / BF CLI)

## Languages and runtimes

| Language | Runtime |
|---|---|
| C# / .NET | **.NET 8** (this project's standard; .NET 6 minimum) |
| JavaScript / TypeScript | Node.js **20+** |
| Python | **3.10+** (3.11+ recommended) |

Java is not on the roadmap. Existing BF-Java bots have no first-party migration path.

## The three problems it solves

1. **Multi-channel reach.** Users live on Teams, M365 Copilot, web, Slack, etc. The SDK normalizes channel-specific payloads into a single `Activity` model.
2. **AI vendor flexibility.** The SDK does not pick a model or orchestrator. Plug in Microsoft Agent Framework (the recommended path — successor to Semantic Kernel + AutoGen), Azure AI Foundry, LangChain, OpenAI Agents, or custom.
3. **Conversation state.** First-class turn + state + storage abstractions.

## How it fits together — request lifecycle

```
Channel → Hosting layer → AgentApplication → Your handlers
```

1. Hosting layer (ASP.NET Core / aiohttp / Express) authenticates the HTTP request.
2. SDK normalizes the payload into an `Activity`.
3. `AgentApplication` evaluates routes and invokes the matching handler.
4. Turn state is loaded before handlers run, saved automatically afterward.
5. Handlers send response activities.
6. Turn ends, `TurnContext` is disposed.

## Channels you can reach

Through the **same** Azure Bot registration the new SDK reuses, **plus** M365 Copilot as a first-class channel:

- Microsoft 365 Copilot
- Microsoft Teams
- Web (DirectLine / Web Chat)
- Email
- SMS
- Slack
- Facebook Messenger
- 10+ more via Azure Bot Service

### Channel-specific behaviors worth knowing now

- **Microsoft 365 Copilot:** streaming responses required; typing activities **not supported**; rich-card support is limited. Plan UI around message + citations + minimal cards.
- **Teams:** full Adaptive Cards, message updates and deletions, invoke activities for task modules, channel-data for mentions / meetings.
- **Non-Microsoft channels (Slack, Facebook, …):** rich content varies. Always check channel-specific docs.

Details: [`../research/09-custom-engine-agents-and-channels.md`](../research/09-custom-engine-agents-and-channels.md).

## When to pick Agents SDK vs. alternatives

| Choose | When |
|---|---|
| **Microsoft 365 Agents SDK** | You need fine-grained control of model and orchestrator. You're migrating from BF SDK. You're invested in Microsoft Agent Framework / Semantic Kernel / LangChain. **← the target for this reference.** |
| **Teams SDK (Teams AI Library)** | Teams-only collaborative agent. Built-in Action Planner orchestrator is good enough. |
| **Microsoft Copilot Studio** | Low-code, business-user-authored agents. SaaS-managed. |
| **Foundry agent** (portal or via Toolkit) | Hosted agent service + model catalog. Good for AI-heavy scenarios already running in Foundry. |

Full comparison: [`../research/09-custom-engine-agents-and-channels.md`](../research/09-custom-engine-agents-and-channels.md).

## Core abstractions

| Concept | One-liner |
|---|---|
| `Activity` | Normalized JSON message — type, channel, sender, recipient, payload. Every interaction is an activity. |
| `TurnContext` | Per-turn snapshot — the activity, adapter, services, send/reply utilities. Disposed after the turn. |
| `AgentApplication` | The agent itself — entry point for all activity. You register handlers (routes) on it. Successor to `ActivityHandler`. |
| Routes | `(selector, handler)` pairs. SDK evaluates routes in a fixed order; ranks break ties. |
| Turn state | Three scopes: Conversation (per-conversation), User (per-user), Temp (current turn only). |
| `CloudAdapter` | The only adapter. `BotFrameworkAdapter` is removed. |
| `IStorage` | Storage abstraction. Memory / Blob / Cosmos providers under `Microsoft.Agents.Storage.*`. |
| Middleware | Still supported via DI; prefer the new `OnBeforeTurn` / `OnAfterTurn` / `OnTurnError` lifecycle hooks. |

## What the SDK is NOT

Direct from the overview page, paraphrased:

> The Agents SDK isn't an AI model, an orchestration engine, or a no-code builder. The Agents SDK doesn't decide what an agent says. That's the job of whatever AI service or business logic the developer wires into the agent.

Bottom line: **"Agents SDK" ≠ "AI".** It's a framework. The AI piece is whatever you plug in.

## Next

→ [`03-migration-mapping.md`](./03-migration-mapping.md)

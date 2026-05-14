# Microsoft 365 Agents SDK — overview

Sourced from https://learn.microsoft.com/microsoft-365/agents-sdk/agents-sdk-overview and https://learn.microsoft.com/microsoft-365/copilot/extensibility/m365-agents-sdk. Verified 2026-05-14.

## One-line definition

The Microsoft 365 Agents SDK is a development framework for conversational agents. It is the **plumbing between a user message (Teams, web, Slack, Copilot, …) and your business / AI logic** — channel abstraction, state management, message routing, with no opinion on which AI model or orchestration framework you use.

## Supported languages and runtimes

| Language | Minimum runtime |
|---|---|
| C# | .NET 8.0 SDK |
| JavaScript / TypeScript | Node.js 18+ (Node 20+ called out in the Node migration guide) |
| Python | Python 3.9 – 3.11 per the overview; the Python migration guide states 3.10+ recommended (3.11+) with support up to 3.14 |

Java is not on the Agents SDK roadmap. Existing BF-Java bots have no first-party migration path.

## The three problems the SDK solves

Direct from MS Learn, paraphrased:

1. **Multi-channel reach.** Users live on different surfaces (Teams, M365 Copilot, web, Slack, Facebook Messenger, …). The SDK normalizes incoming and outgoing traffic into a single `Activity` model so agent logic doesn't have N integrations.
2. **AI vendor flexibility.** The SDK is AI-agnostic. It does not pick a model or orchestrator for you. Plug in Microsoft Agent Framework (the successor to Semantic Kernel + AutoGen, and the recommended path), Azure AI Foundry, OpenAI Agents, LangChain, custom — your call.
3. **Conversation state.** First-class turn + state + storage abstractions so you don't reinvent persistence.

## How it fits together — request lifecycle

`Channel → Hosting layer → AgentApplication → Your handlers`

When a message arrives:

1. The hosting layer (ASP.NET Core / aiohttp / Express) receives the HTTP request and authenticates it.
2. The SDK normalizes the channel-specific payload into an `Activity`.
3. `AgentApplication` evaluates registered routes and invokes the matching handler.
4. Turn state is loaded before handlers run and saved automatically afterward.
5. Handlers send response activities back through the same channel.
6. Turn ends, `TurnContext` is disposed.

Source: https://learn.microsoft.com/microsoft-365/agents-sdk/agent-application

## What the SDK is NOT

From the overview page, verbatim points:

> The Agents SDK isn't an AI model, an orchestration engine, or a no-code builder. The Agents SDK doesn't decide what an agent says. These elements are the job of whatever AI service or business logic the developer wires into the agent.

That distinction matters in practice:

- "Agents SDK" ≠ "AI" — it's a framework. The AI part is whatever the team wires in (Microsoft Agent Framework, Foundry, …).
- It is the **pro-code** option. Copilot Studio is the low-code option, Teams SDK is the Teams-collaborative option, Foundry is the model + agent service option.

## When to choose the Agents SDK (vs. alternatives)

From https://learn.microsoft.com/microsoft-365/copilot/extensibility/m365-agents-sdk, paraphrased:

- You need fine-grained control over model and orchestrator selection.
- You want to leverage prior Bot Framework experience.
- You're already invested in Microsoft Agent Framework, Semantic Kernel, or LangChain.

Compared to:

- **Copilot Studio** — low-code, fully-managed, fastest to ship; you give up orchestration control.
- **Teams SDK / Teams AI Library** — Teams-specific, built-in Action Planner orchestrator; you give up multi-channel.
- **Foundry agents (published via portal or proxied via Agents Toolkit)** — model and agent service hosted in Foundry; surfaces in M365 Copilot and Teams.

See [`09-custom-engine-agents-and-channels.md`](./09-custom-engine-agents-and-channels.md) for the full comparison table.

## Core abstractions to know before reading the migration deep-dives

| Concept | One-liner |
|---|---|
| `Activity` | The normalized JSON message — type, channel, sender, recipient, payload. Every interaction is an activity. |
| `TurnContext` | Per-turn snapshot — the activity, adapter, services, send/reply utilities. Created on each incoming activity, disposed after the turn. |
| `AgentApplication` | The agent itself — entry point for all activity. You register handlers (routes) on it. Successor to `ActivityHandler`. |
| Routes | `(selector, handler)` pairs. The SDK evaluates routes in a fixed order (invoke routes first, then agentic routes, then everything else; ranks break ties within a group). |
| Turn state | Three scopes: Conversation (per-conversation, persisted), User (per-user, persisted), Temp (current turn only). Loaded before handlers, saved after. |
| `CloudAdapter` | The only adapter. `BotFrameworkAdapter` is removed. |
| `IStorage` | Storage abstraction. Memory / Blob / Cosmos providers under `Microsoft.Agents.Storage.*`. |
| Middleware | Still supported; in .NET register `IMiddleware` via DI. Prefer the new `OnBeforeTurn` / `OnAfterTurn` / `OnTurnError` lifecycle hooks for new code. |

Sources:
- https://learn.microsoft.com/microsoft-365/agents-sdk/activity-protocol
- https://learn.microsoft.com/microsoft-365/agents-sdk/agent-application

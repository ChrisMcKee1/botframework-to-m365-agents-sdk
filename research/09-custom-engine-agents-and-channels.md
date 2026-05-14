# Custom engine agents, channels, and tool comparison

Sources:
- https://learn.microsoft.com/microsoft-365/copilot/extensibility/overview-custom-engine-agent
- https://learn.microsoft.com/microsoft-365/copilot/extensibility/m365-agents-sdk
- https://learn.microsoft.com/microsoft-365/copilot/extensibility/create-deploy-agents-sdk
- https://learn.microsoft.com/microsoft-365/agents-sdk/activity-protocol (channel-specific considerations)

Verified 2026-05-14.

## What is a "custom engine agent"?

A **custom engine agent (CEA)** is a Microsoft 365 Copilot agent where the developer brings their own orchestration and AI services. Contrast with **declarative agents**, which are configured (not coded) and run on Microsoft's orchestration. CEAs give:

- Custom orchestration (your code, your AI stack)
- Flexible AI models (foundation, fine-tuned, industry-specific)
- Proactive automation (programmatically trigger workflows)

A custom engine agent is the **target** when you migrate a Bot Framework bot — the Agents SDK is one of the four ways to build one.

## Four development approaches (the canonical comparison table)

| Aspect | Copilot Studio | Teams SDK | **Microsoft 365 Agents SDK** | Foundry (via portal or Toolkit) |
|---|---|---|---|---|
| **Approach** | Low-code | Pro-code | **Pro-code** | Low-code or Pro-code |
| **Tooling** | Copilot Studio UI | VS Code / VS + Teams SDK | **VS Code / VS + Agents Toolkit** | Foundry Portal, or VS Code / VS + Agents Toolkit |
| **Publishing** | My org | My org / ISV store | **My org / ISV store / 10+ channels** | My org / ISV store |
| **Channels** | M365 Copilot, Teams, partner apps, mobile apps, custom websites | M365 Copilot, Teams | **M365 Copilot, Teams, partner apps, mobile apps, custom websites** | M365 Copilot, Teams (others via custom integration) |
| **Productivity** | Individual | Group | **Group** | Individual |
| **Orchestrator** | Copilot Studio | Teams AI Action Planner | **Bring your own** (Microsoft Agent Framework, SK, LangChain, …) | Bring your own (Microsoft Agent Framework, SK, LangChain, …) |
| **AI Models** | Copilot Studio's | Any | **Any** | Foundry OpenAI or custom |
| **Languages** | N/A (low-code) | C#, TS, JS, Python | **C#, JS, Python** | Python, C# |

(Bolded column is the one this project targets.)

## When to choose Agents SDK specifically

From `m365-agents-sdk` page:

- You need fine-grained control over model and orchestrator selection.
- You want to leverage prior Bot Framework experience.
- You're familiar with Microsoft Agent Framework, Semantic Kernel, or LangChain.

For the typical migration case: existing BF investment + pro-code team + likely multi-channel future → Agents SDK is the right column.

## Channels reachable through the Agents SDK

Through the Azure Bot registration the new SDK reuses, **plus** the new M365 Copilot first-class channel:

- Microsoft 365 Copilot
- Microsoft Teams
- Web (DirectLine, Web Chat)
- Email
- SMS
- Slack
- Facebook Messenger
- … and more via Azure Bot Service

The Agents Toolkit can publish to 10+ messaging channels.

## Channel-specific behaviors to know

From the Activity Protocol page:

### Microsoft Teams

- Rich Adaptive Cards with advanced features
- Message updates and deletions
- Channel-specific data (mentions, meeting info)
- Invoke activities for task modules (`task/fetch`, `task/submit`)

### Microsoft 365 Copilot

- **Streaming responses are required**
- Citations and references supported
- Primarily message activities
- Limited support for rich / adaptive cards
- **Typing activities NOT supported in M365 Copilot** (they work in Teams)

### Web Chat / DirectLine

- Full support for all activity types
- Supports custom channel data

### Non-Microsoft channels (Slack, Facebook, …)

- May have limited activity-type coverage
- Card rendering varies (often unsupported / different)
- Always check channel-specific docs

## Streaming behavior in Teams + M365 Copilot — gotchas

Direct from the design considerations:

- Use **one** `StreamingResponse` per user turn. Finalize with `endStream()` before sending anything else.
- Attach media inside the stream via `setAttachments()` — don't send a separate non-streaming activity (timestamp ordering will bite you).
- Don't start a new stream before the previous one is finalized.
- Serialize outgoing messages — don't fan out parallel `sendActivity` calls.
- After `endStream()`, anything new becomes a separate activity that may appear out of order. Use `replyToId` to keep follow-ups in-thread.

This matters when an agent surfaces in M365 Copilot — Copilot is streaming-by-design.

## Foundry as an integration option (not a competitor)

Foundry agents can be **integrated into** M365 Copilot via two paths:

| Path | Tooling | Best for |
|---|---|---|
| Publish to M365 from Foundry portal | Foundry portal | Rapid deployment, minimal code changes; portal auto-provisions Azure Bot Service + Entra ID |
| Proxy app via Agents Toolkit | VS Code / VS + Agents Toolkit | Advanced customization, SSO, managed infrastructure, multi-environment deployment |

If the team already runs logic in Foundry, the Agents-SDK proxy pattern is the Microsoft-recommended bridge — but only as a phase-2 step, not the migration baseline.

## App manifest requirement

> Custom engine agents are supported in **app manifest version 1.21 and later**.

When packaging for Teams / M365 Copilot, the Teams app manifest must be ≥1.21. Older manifests need an update before publish.

## Responsible AI / privacy / compliance

Worth knowing but out of scope for this repo:

- Custom engine agent prompts and responses in Copilot Chat / Teams are stored under M365 product terms.
- Admins can use Content Search / Microsoft Purview to manage stored interactions.
- Pre-publish review: [Responsible AI principles](https://learn.microsoft.com/azure/well-architected/ai/responsible-ai) and store publishing requirements for ISVs (if they publish to the Commercial Store).

# Microsoft 365 Agents SDK — Bot Framework migration reference

> A **runnable side-by-side** for migrating a Bot Framework SDK v4 codebase to the Microsoft 365 Agents SDK. Built as a reusable reference for pro-code dev teams.

Bot Framework SDK is **archived**. Support ended **December 31, 2025**. The forward path for any .NET / Node / Python bot you ship today is the [Microsoft 365 Agents SDK](https://learn.microsoft.com/microsoft-365/agents-sdk/). This repo demonstrates the migration without speculation — every claim cites [Microsoft Learn](research/sources.md).

## 5-minute tour

1. **Skim** [`docs/03-migration-mapping.md`](docs/03-migration-mapping.md) for the canonical .NET / Node / Python deltas.
2. **Open** [`samples/before-bot-framework/`](samples/before-bot-framework/) in VS Code. Run with **Bot Framework Emulator**.
3. **Open** [`samples/after-agents-sdk/`](samples/after-agents-sdk/) in VS Code. Run with **Microsoft 365 Agents Playground**.
4. **Diff** them with [`samples/side-by-side/`](samples/side-by-side/) — the annotated, file-by-file walk-through.
5. **Plan** your own migration with [`docs/07-discovery-checklist.md`](docs/07-discovery-checklist.md) and [`docs/08-migration-playbook.md`](docs/08-migration-playbook.md).

That's the whole repo. The docs are a migration playbook, not a textbook.

## What's in here

```
.
├── README.md                            ← you are here
├── AGENTS.md                            ← ground rules for AI agents working in this repo
├── docs/                                ← migration playbook (read in order)
│   ├── 01-bot-framework-overview.md     ← state of BF SDK + forward paths
│   ├── 02-agents-sdk-overview.md        ← what the Agents SDK is (and isn't)
│   ├── 03-migration-mapping.md          ← .NET / Node / Python find-and-replace tables (CANONICAL)
│   ├── 04-tooling.md                    ← Toolkit / Playground / Emulator / CLI cleanup
│   ├── 05-auth-and-azure-resources.md   ← TokenValidation + Connections + 8 AuthType examples
│   ├── 06-ai-orchestration-options.md   ← Agent Framework / Foundry / SK / OpenAI Agents / LangChain / Custom
│   ├── 07-discovery-checklist.md        ← discovery checklist for sizing a migration (sections A–H)
│   ├── 08-migration-playbook.md         ← 10-phase end-to-end checklist
│   └── 09-running-in-teams.md           ← provision once, sideload, swap SDKs to demo the migration
├── samples/
│   ├── before-bot-framework/            ← BF SDK v4, .NET 8 — builds clean
│   ├── after-agents-sdk/                ← Agents SDK, .NET 8 — builds clean
│   └── side-by-side/                    ← annotated file-by-file diff
└── research/                            ← sourcing + research notes
    └── sources.md                       ← authoritative MS Learn link list
```

## Reference scenario

Both samples implement the same thing:

- A bot/agent that greets new conversation members with an Adaptive Card
- A 2-step waterfall dialog that asks for the user's name and confirms it
- After confirmation, subsequent messages echo as `"[name] said: [text]"`

That's it. The point is to show that the *scenario* migrates with mechanical edits — different packages, namespaces, hosting, config. No re-architecting required.

## Audience

- **Pro-code dev teams** (.NET / Node / Python) maintaining an existing Bot Framework SDK v4 solution.
- **Solution architects / leadership** who need confidence that the migration path is sound and that Azure resources (Bot registration, App ID, channels) are preserved.
- **Migration leads** who need a reusable planning artifact before sizing work.

## What's in scope

- A runnable **before vs. after** in C# / .NET 8 with the same external behavior, so the deltas are concrete instead of theoretical.
- A migration **playbook** that maps every concept a typical BF v4 codebase has to its Agents SDK replacement (packages, namespaces, types, configuration, auth, hosting, state, dialogs/middleware).
- A **tooling chain** writeup (Microsoft 365 Agents Toolkit for VS / VS Code, Microsoft 365 Agents Playground, Bot Framework Emulator for legacy debug).
- A **discovery checklist** that can be worked through in ~30 minutes to scope an actual migration.
- Direction on the **AI / orchestration** story — how Azure AI Foundry / Microsoft Agent Framework slot into the same agent to retire LUIS/QnA dependencies.

## What's not in scope

- Migrating any specific production codebase. This is a reference.
- Net-new agent capabilities. The goal is to demonstrate the migration, not extend the scenario.
- Copilot Studio (low-code) or pure Teams AI Library paths in depth. They're mentioned as alternatives but not the focus.
- JavaScript and Python sample projects — language deltas are documented in [`docs/03-migration-mapping.md`](docs/03-migration-mapping.md) but only C# / .NET 8 ships as a runnable sample.
- A Java migration path — Java isn't on the Agents SDK roadmap.

## Prerequisites

- **.NET 8 SDK** (or .NET 9 / 10 with rollforward) — both samples target `net8.0`
- **VS Code** with **Microsoft 365 Agents Toolkit** extension (`TeamsDevApp.ms-teams-vscode-extension`) — for the after sample
- **Bot Framework Emulator** — for the before sample
- **Microsoft 365 Agents Playground** — for the after sample (`npm install -g @microsoft/teams-app-test-tool`)

> The Microsoft 365 Agents Toolkit does **not** support publishing to Microsoft 365 Government tenants. Plan accordingly. See [`docs/04-tooling.md`](docs/04-tooling.md).

## Build and run

```pwsh
# Bot Framework SDK v4 sample
cd samples/before-bot-framework
dotnet build
dotnet run

# Microsoft 365 Agents SDK sample
cd ../after-agents-sdk
dotnet build
dotnet run
```

Each sample binds a different Kestrel port via its `Properties/launchSettings.json` so you can run **both at once**:

- **before-bot-framework** → `http://localhost:5000/api/messages`
- **after-agents-sdk** → `http://localhost:5001/api/messages`

See each sample's `README.md` for tool-specific instructions, and [`docs/09-running-in-teams.md`](docs/09-running-in-teams.md) for the full Azure Bot + Teams sideload walkthrough that makes both samples talk to the same Teams app.

## License

Code adapted from public Microsoft Learn samples retains the upstream MIT license terms. Treat the rest of the repo as reference material — fork it for your own migration.

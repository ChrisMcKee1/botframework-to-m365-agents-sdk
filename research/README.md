# Research notes

Raw research backing the migration playbook. All facts here are cited to MS Learn in [`sources.md`](./sources.md). When `docs/` is written, it pulls from this folder.

Reading order:

1. [`01-bf-end-of-life-and-status.md`](./01-bf-end-of-life-and-status.md) — why this migration exists, what's archived, what still runs
2. [`02-agents-sdk-overview.md`](./02-agents-sdk-overview.md) — what the M365 Agents SDK is, languages, core concepts
3. [`03-unsupported-and-deprecated.md`](./03-unsupported-and-deprecated.md) — features in BF that do not migrate
4. [`04-dotnet-migration-deep-dive.md`](./04-dotnet-migration-deep-dive.md) — C# packages, namespaces, types, `Program.cs`, state, JSON, troubleshooting
5. [`05-python-migration-deep-dive.md`](./05-python-migration-deep-dive.md) — Python packages, imports, env vars, `AgentApplication` decorator pattern
6. [`06-nodejs-migration-deep-dive.md`](./06-nodejs-migration-deep-dive.md) — Node/JS packages, imports, `AuthConfiguration`, `ActivityHandler` → `AgentApplication`
7. [`07-tooling.md`](./07-tooling.md) — Agents Toolkit (VS / VS Code / CLI), Agents Playground, Emulator
8. [`08-authentication-msal.md`](./08-authentication-msal.md) — MSAL auth types, `TokenValidation`, `Connections`, MSAL config
9. [`09-custom-engine-agents-and-channels.md`](./09-custom-engine-agents-and-channels.md) — Custom engine agents, channels, tool comparison (Studio / Teams / Agents / Foundry)
10. [`10-ai-orchestration-options.md`](./10-ai-orchestration-options.md) — Microsoft Agent Framework, Foundry, Semantic Kernel, LangChain, OpenAI Agents — how to choose
11. [`11-activity-protocol-and-agentapplication.md`](./11-activity-protocol-and-agentapplication.md) — programming-model primer
12. [`99-cheatsheet.md`](./99-cheatsheet.md) — all rename tables in one place

[`sources.md`](./sources.md) is the authoritative citation index. Add new sources there before referencing them elsewhere.

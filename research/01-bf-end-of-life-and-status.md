# Bot Framework SDK — end-of-life status

Verified against MS Learn on 2026-05-14. See [`sources.md`](./sources.md).

## Headline facts

- **The Bot Framework SDK and Bot Framework Emulator have been archived on GitHub.** The repositories are no longer updated or maintained.
- **Support tickets for the Bot Framework SDK are no longer serviced as of December 31, 2025.** Microsoft will not open new investigations into BF SDK issues via Azure portal support.
- **Existing workloads continue to run.** Archival ≠ deletion. Customers' bots keep functioning — no hard cutoff for runtime, only for support and updates.
- **New V3 bot creation is already disabled.** V4 is the only supported generation of the SDK, and even V4 is now archived.

Sources:
- https://learn.microsoft.com/azure/bot-service/what-is-new
- https://learn.microsoft.com/azure/bot-service/bot-service-resources-links-help

## What Microsoft now recommends instead

Three explicitly-named forward paths in the BF EOL notices:

| Forward path | When MS recommends it | Notes |
|---|---|---|
| **Microsoft 365 Agents SDK** | "Build agents with your choice of AI services, orchestration, and knowledge." | C#, JavaScript, Python. Direct migration target for existing BF code. `aka.ms/agents` → https://github.com/Microsoft/Agents |
| **Teams SDK (Teams AI Library)** | "Building a collaborative agent designed to work within Microsoft Teams." | Teams-specific APIs, adaptive cards, built-in AI orchestration. |
| **Microsoft Copilot Studio** | "SaaS-based agent platform." | Low-code, fully managed. |

For a team with an existing pro-code BF v4 bot, the **Microsoft 365 Agents SDK** is the relevant target. The other two are alternatives to acknowledge but not size against.

## What still works during/after migration

The migration guides are explicit that the underlying Azure plumbing is preserved:

- **Existing Azure Bot registration stays.** Same resource, same App ID, same secret.
- **Channels stay.** Same Teams / Web Chat / Slack / etc. channel registrations bound to that bot.
- **Hosting stays.** App Service / Functions / wherever the bot is deployed today continues to host the new SDK.
- **Legacy app settings (`MicrosoftAppType`, `MicrosoftAppId`, `MicrosoftAppPassword`, `MicrosoftAppTenantId`) are harmless during migration**, but the new SDK doesn't read them. Remove them after validating the new `TokenValidation` + `Connections` blocks.

Sources:
- https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-dotnet (§ Azure resources)
- https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-python (§ Azure resources)
- https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-nodejs (§ Azure resources)

## What this means in practice

- This is **modernization, not crisis**. The bot still runs. There is no Microsoft-imposed runtime deadline.
- The pressure is: **no new security patches, no support tickets, no new features**. That's a business-risk argument, not a technical-failure argument.
- The migration path is **incremental** — same Azure Bot, same App ID. The production bot can keep running while the Agents-SDK version is built in parallel.
- **Composer / Adaptive Dialogs / LUIS / QnA are the real risk vectors** because they don't migrate and may already be unsupported runtime services. See [`03-unsupported-and-deprecated.md`](./03-unsupported-and-deprecated.md).

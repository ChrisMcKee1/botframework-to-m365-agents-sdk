# Research notes & sources

Authoritative citation index. All Microsoft Learn unless noted. Verified **2026-05-14**.

Add new sources here **before** referencing them from `research/*.md` or `docs/*.md`.

## Bot Framework status (EOL)

- BF SDK + Emulator archived on GitHub. Support tickets no longer serviced as of **Dec 31, 2025**. New V3 bot creation already disabled. Existing workloads continue to run.
- https://learn.microsoft.com/azure/bot-service/bot-service-resources-links-help
- https://learn.microsoft.com/azure/bot-service/what-is-new
- https://learn.microsoft.com/azure/bot-service/bot-service-resources-faq-availability

## M365 Agents SDK — overview & programming model

- *What is the Microsoft 365 Agents SDK* — https://learn.microsoft.com/microsoft-365/agents-sdk/agents-sdk-overview
- *Build custom engine agents with M365 Agents SDK* — https://learn.microsoft.com/microsoft-365/copilot/extensibility/m365-agents-sdk
- *Create and deploy an agent with M365 Agents SDK* — https://learn.microsoft.com/microsoft-365/copilot/extensibility/create-deploy-agents-sdk
- *Custom engine agents overview (tool comparison)* — https://learn.microsoft.com/microsoft-365/copilot/extensibility/overview-custom-engine-agent
- *Understanding Activity Protocol* — https://learn.microsoft.com/microsoft-365/agents-sdk/activity-protocol
- *AgentApplication* — https://learn.microsoft.com/microsoft-365/agents-sdk/agent-application
- Activity Protocol spec on GitHub — https://github.com/microsoft/Agents/blob/main/specs/activity/protocol-activity.md
- aka.ms/agents → https://github.com/Microsoft/Agents

## Migration guidance

- *BF → Agents SDK migration overview + unsupported packages* — https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-guidance
- *.NET migration guide* — https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-dotnet
- *Python migration guide* — https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-python
- *Node.js migration guide* — https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-nodejs

## Tooling

- *Microsoft 365 Agents Toolkit (VS / VS Code)* — https://learn.microsoft.com/microsoftteams/platform/toolkit/overview-agents-toolkit
- VS Code Marketplace listing — https://marketplace.visualstudio.com/items?itemName=TeamsDevApp.ms-teams-vscode-extension
- *M365 Agents Playground* — npm `@microsoft/teams-app-test-tool` — https://www.npmjs.com/package/@microsoft/teams-app-test-tool
- *Debug with Agents Playground* — https://learn.microsoft.com/microsoftteams/platform/toolkit/debug-your-teams-app-test-tool
- *Create a new agent (Visual Studio, C#)* — https://learn.microsoft.com/microsoft-365/agents-sdk/create-new-toolkit-project-vs
- *Create a new agent (VS Code)* — https://learn.microsoft.com/microsoft-365/agents-sdk/create-new-toolkit-project-vsc
- *Agents SDK quickstart* — https://learn.microsoft.com/microsoft-365/agents-sdk/create-test-basic-agent
- *AspNetExtensions sample (copy into .NET projects)* — https://github.com/microsoft/Agents/blob/main/samples/dotnet/quickstart/AspNetExtensions.cs

## Authentication & MSAL (.NET)

- *Configure authentication in a .NET agent (MSAL options)* — https://learn.microsoft.com/microsoft-365/agents-sdk/microsoft-authentication-library-configuration-options
- *Configure authentication in JavaScript* — https://github.com/microsoft/Agents/blob/main/docs/HowTo/azurebot-auth-for-js.md

## Deployment

- *Deploy your agent to Azure manually* — https://learn.microsoft.com/microsoft-365/agents-sdk/deploy-azure-bot-service-manually
- *Publish your Foundry agent to Microsoft 365* — https://learn.microsoft.com/azure/ai-foundry/agents/how-to/publish-copilot
- *Integrate your Foundry agent with Microsoft Agent Toolkit* — https://aka.ms/aif2m365-procode

## Storage & state

- *Use storage providers in your agent* — https://learn.microsoft.com/microsoft-365/agents-sdk/storage
- *Manage state in Agents SDK* — https://learn.microsoft.com/microsoft-365/agents-sdk/state-concepts
- *Managing Turns in the Agents SDK* — https://learn.microsoft.com/microsoft-365/agents-sdk/managing-turns

## Adjacent platforms (referenced, not the target)

- *Teams SDK (Teams AI Library)* — https://learn.microsoft.com/microsoftteams/platform/teams-sdk/
- *Microsoft Copilot Studio* — https://www.microsoft.com/microsoft-copilot/microsoft-copilot-studio
- *Microsoft Foundry overview* — https://learn.microsoft.com/azure/ai-foundry/what-is-azure-ai-foundry
- *Microsoft Agent Framework — overview* — https://learn.microsoft.com/agent-framework/overview/
- *Use Semantic Kernel and Agent Framework in Agents SDK* — https://learn.microsoft.com/microsoft-365/agents-sdk/using-semantic-kernel-agent-framework
- *Semantic Kernel to Agent Framework migration guide* — https://learn.microsoft.com/agent-framework/migration-guide/from-semantic-kernel/
- *Agent Framework integrations (release-status matrix)* — https://learn.microsoft.com/agent-framework/integrations/
- *Semantic Kernel (predecessor — still supported, official migration path to Agent Framework)* — https://learn.microsoft.com/semantic-kernel/overview/
- LangChain (third-party) — https://www.langchain.com/

## Unsupported features in Agents SDK (must replace or remove)

- Adaptive Dialogs
- AdaptiveExpressions (still callable, unsupported)
- Bot Framework Composer + artifacts
- LUIS / QnA Maker / Orchestrator
- Language Generation (LG)
- Language Understanding parsers (`Microsoft.Bot.Builder.Parsers.LU`)
- `BotFrameworkAdapter` (use `CloudAdapter`)
- ASP.NET WebAPI (.NET — use ASP.NET Core minimal APIs)
- Bot Framework CLI (`bf`)
- Generators (Yeoman, etc.)
- Inspection middleware
- Streaming Connections (legacy)
- `QueueStorage` (BotBuilder)
- `TemplateManager`
- App Insights bot-telemetry helpers
- Deprecated activities (payments, etc.)

## Key gotchas (verified against MS Learn)

- **Government tenants** cannot publish agents via Agents Toolkit (https://learn.microsoft.com/microsoft-365/copilot/extensibility/m365-agents-sdk).
- **Azure resources stay** — Azure Bot registration, App ID, secret reused.
- **Legacy app settings** (`MicrosoftAppType`, `MicrosoftAppId`, `MicrosoftAppPassword`, `MicrosoftAppTenantId`) are harmless during migration; remove after validating new `TokenValidation` + `Connections`.
- **.NET:** fully-qualify `IStorage` / `IMiddleware` when ambiguous (`Microsoft.Agents.Storage.IStorage`).
- **.NET:** `TurnContext.TurnState` → `TurnContext.Services` (`.Services.Get<T>()`).
- **.NET:** replace `JObject`/`JToken` with `System.Text.Json` `JsonDocument`/`JsonElement`.
- **.NET:** drop `AddNewtonsoftJson` from `Program.cs` unless other code needs it.
- **Python:** imports use **underscores** — `microsoft_agents`, not `microsoft.agents`. Single most common migration mistake.
- **Python:** env config uses **double-underscore** hierarchical naming (`CONNECTIONS__SERVICE_CONNECTION__SETTINGS__CLIENTID`).
- **Python:** `TurnContext` static helpers (`apply_conversation_reference`, etc.) moved onto `Activity` instances.
- **Node:** env vars renamed to lowercase (`MicrosoftAppId` → `clientId`).
- **Node:** `ActivityHandler` is supported but **deprecated** in favor of `AgentApplication`.
- **Node:** `ConfigurationBotFrameworkAuthentication` → `AuthConfiguration` interface, loaded via `loadAuthConfigFromEnv`.
- **M365 Copilot channel:** streaming responses required, typing activities NOT supported, rich-card support limited.
- **Teams app manifest:** must be ≥ **1.21** to support custom engine agents.
- **Foundry integration option:** two paths — publish from Foundry portal, or proxy via Agents Toolkit (recommended for SSO / multi-environment).

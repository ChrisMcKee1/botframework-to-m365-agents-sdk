# Migration cheatsheet — one page

All rename tables in one place. Sourced from the language-specific MS Learn pages — see [`sources.md`](./sources.md). Use as a single-page reference.

## .NET packages

| Bot Framework SDK | Microsoft 365 Agents SDK |
|---|---|
| `Microsoft.Bot.Builder.Integration.AspNet.Core` | `Microsoft.Agents.Hosting.AspNetCore` + `Microsoft.Agents.Authentication.Msal` |
| `Microsoft.Bot.Builder` | `Microsoft.Agents.Builder` |
| `Microsoft.Bot.Builder.Dialogs` | `Microsoft.Agents.Builder.Dialogs` |
| `Microsoft.Bot.Builder.Azure.Blobs` | `Microsoft.Agents.Storage.Blobs` |
| `Microsoft.Bot.Builder.Azure` | `Microsoft.Agents.Storage.CosmosDb` |
| `Microsoft.Bot.Schema` | `Microsoft.Agents.Core` |
| Teams: `Microsoft.Bot.Builder.Teams` | `Microsoft.Agents.Extensions.Teams` + `.Teams.Compat` |
| `Newtonsoft.Json` | Remove. Use `System.Text.Json`. |

## .NET namespaces

| Old | New |
|---|---|
| `using Microsoft.Bot.Builder.Integration.AspNet.Core;` | `using Microsoft.Agents.Hosting.AspNetCore;` |
| `using Microsoft.Bot.Builder;` | `using Microsoft.Agents.Builder;` |
| `Microsoft.Bot.Builder.Dialogs` | `Microsoft.Agents.Builder.Dialogs` |
| `using Microsoft.Bot.Schema;` | `using Microsoft.Agents.Core.Models;` |
| `Microsoft.Bot.Connector.Authentication` | `Microsoft.Agents.Connector` |
| `using Microsoft.Bot.Builder.Teams;` | `using Microsoft.Agents.Extensions.Teams.Compat;` |
| `using Microsoft.Bot.Schema.Teams;` | `using Microsoft.Agents.Extensions.Teams.Models;` |
| `using Newtonsoft.Json;` | `using System.Text.Json;` |
| `using Newtonsoft.Json.Linq;` | Remove |

## .NET types

| Old | New |
|---|---|
| `BotState` | `AgentState` |
| `OAuthPromptSettings.ConnectionName` | `OAuthPromptSettings.AzureBotOAuthConnectionName` |
| `IAttachments.GetAttachmentInfoWithHttpMessagesAsync` | `IAttachments.GetAttachmentInfoAsync` |
| `IBotFrameworkHttpAdapter` | `IAgentHttpAdapter` |
| `BotAdapter` | `ChannelAdapter` |
| `CloudAdapterBase` | `ChannelServiceAdapterBase` |
| `TurnContext.TurnState.Get<T>` | `TurnContext.Services.Get<T>` |
| `BotFrameworkAdapter` | **Removed.** Use `CloudAdapter`. |
| `JObject` / `JToken` | `JsonDocument` / `JsonElement` |

## Python packages

| Old | New |
|---|---|
| `botbuilder-core` | `microsoft-agents-hosting-core` |
| `botbuilder-schema` | `microsoft-agents-activity` |
| `botbuilder-azure` | `microsoft-agents-storage-blob` + `microsoft-agents-storage-cosmos` |
| `botbuilder-integration-aiohttp` | `microsoft-agents-hosting-aiohttp` |
| Teams | `microsoft-agents-hosting-teams` |
| Auth | `microsoft-agents-authentication-msal` |

## Python imports — dots → underscores

| Old | New |
|---|---|
| `from botbuilder.core import …` | `from microsoft_agents.hosting.core import …` |
| `from botbuilder.schema import …` | `from microsoft_agents.activity import …` |
| `from botbuilder.integration.aiohttp import …` | `from microsoft_agents.hosting.aiohttp import …` |
| `from botbuilder.core.teams import …` | `from microsoft_agents.hosting.teams import …` |

## Python types

| Old | New |
|---|---|
| `BotState` | `AgentState` |
| `BotFrameworkAdapter` | `CloudAdapter` |
| `BotFrameworkHttpClient` | `AgentHttpClient` |
| `OAuthPromptSettings.connection_name` | `OAuthPromptSettings.azure_bot_oauth_connection_name` |
| `turn_context.turn_state` | `turn_context.services` |
| `TurnContext.apply_conversation_reference()` | `activity.apply_conversation_reference()` |
| `TurnContext.get_conversation_reference()` | `activity.get_conversation_reference()` |
| `TurnContext.get_reply_conversation_reference()` | `activity.get_reply_conversation_reference()` |
| `TurnContext.remove_recipient_mention()` | `activity.remove_recipient_mention()` |
| `TurnContext.get_mentions()` | `activity.get_mentions()` |
| `TurnContext.remove_mention_text()` | `activity.remove_mention_text()` |

## Python env vars — double-underscore hierarchy

| Old | New |
|---|---|
| `APP_ID` / `MICROSOFT_APP_ID` | `CONNECTIONS__SERVICE_CONNECTION__SETTINGS__CLIENTID` |
| `APP_PASSWORD` / `MICROSOFT_APP_PASSWORD` | `CONNECTIONS__SERVICE_CONNECTION__SETTINGS__CLIENTSECRET` |
| `APP_TENANT_ID` / `MICROSOFT_APP_TENANT_ID` | `CONNECTIONS__SERVICE_CONNECTION__SETTINGS__TENANTID` |

## Node packages

| Old | New |
|---|---|
| `botbuilder` | `@microsoft/agents-hosting` |
| `botframework-schema` | `@microsoft/agents-activity` |
| `botbuilder-dialogs` | `@microsoft/agents-hosting-dialogs` |
| `botbuilder-azure` | `@microsoft/agents-hosting-storage-cosmos` |
| `botbuilder-azure-blobs` | `@microsoft/agents-hosting-storage-blob` |
| (manual) | `@microsoft/agents-hosting-express` |
| Teams | `@microsoft/agents-hosting-extensions-teams` |

## Node imports

| Old | New |
|---|---|
| `require('botframework-schema')` | `require('@microsoft/agents-activity')` |
| `require('botbuilder')` | `require('@microsoft/agents-hosting')` |
| `require('botbuilder-dialogs')` | `require('@microsoft/agents-hosting-dialogs')` |

## Node env vars

| Old | New |
|---|---|
| `MicrosoftAppId` | `clientId` |
| `MicrosoftAppPassword` | `clientSecret` |
| `MicrosoftAppTenantId` | `tenantId` |

## Adapters & hosting (all languages)

| Bot Framework SDK | Agents SDK |
|---|---|
| `BotFrameworkAdapter` (any language) | `CloudAdapter` only |
| .NET Startup + controller | .NET minimal API + `builder.AddAgent<T>()` |
| .NET adapter-internal JWT | ASP.NET `AddAgentAspNetAuthentication` reads `TokenValidation` |
| Python aiohttp + manual adapter | `CloudAdapter` + `MsalConnectionManager` + `AgentApplication` decorator |
| Node `ConfigurationBotFrameworkAuthentication` | Node `loadAuthConfigFromEnv` + `authorizeJWT` middleware |
| .NET `MicrosoftAppId` / `MicrosoftAppPassword` settings | `Connections` + `ConnectionsMap` blocks |

## Unsupported in Agents SDK (replace, don't migrate)

- Adaptive Dialogs
- AdaptiveExpressions (still callable but unsupported)
- Bot Framework Composer artifacts
- LUIS / QnA Maker / Orchestrator
- Language Generation (LG)
- `BotFrameworkAdapter` (use `CloudAdapter`)
- ASP.NET WebAPI (.NET only — use ASP.NET Core)
- Bot Framework CLI (`bf`)
- App Insights bot-telemetry helpers
- Streaming Connections (legacy)
- `QueueStorage` (BotBuilder)
- `TemplateManager`
- Deprecated activities (payments, etc.)

## What stays

- Azure Bot registration (same App ID, same secret)
- Channels you've already wired up
- Hosting (App Service / Functions / AKS)
- `ConversationState` / `UserState` / `PrivateConversationState` API shape
- `IStorage` shape (new namespace)
- Dialogs (waterfall, component) via the compat namespaces
- `ActivityHandler` base class as a migration bridge (JS marks it deprecated in favor of `AgentApplication`)
- `IMiddleware` registration via DI (prefer new `OnBeforeTurn` / `OnAfterTurn` hooks)

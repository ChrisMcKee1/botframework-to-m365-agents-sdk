# 03 — Migration mapping (packages, namespaces, types, config)

The canonical rename and re-wire reference. **Start here when planning a migration.**

Detailed walkthroughs per language:
- .NET: [`../research/04-dotnet-migration-deep-dive.md`](../research/04-dotnet-migration-deep-dive.md)
- Python: [`../research/05-python-migration-deep-dive.md`](../research/05-python-migration-deep-dive.md)
- Node.js: [`../research/06-nodejs-migration-deep-dive.md`](../research/06-nodejs-migration-deep-dive.md)

Single-page handout: [`../research/99-cheatsheet.md`](../research/99-cheatsheet.md).

Citations: [`../research/sources.md`](../research/sources.md).

---

## .NET (C#)

Target runtime: **`net8.0`** (project standard; PRD allows `net6.0` minimum).

### Packages — replace

| Bot Framework SDK | Microsoft 365 Agents SDK |
|---|---|
| `Microsoft.Bot.Builder.Integration.AspNet.Core` | `Microsoft.Agents.Hosting.AspNetCore` **and** `Microsoft.Agents.Authentication.Msal` |
| `Microsoft.Bot.Builder` | `Microsoft.Agents.Builder` |
| `Microsoft.Bot.Builder.Dialogs` | `Microsoft.Agents.Builder.Dialogs` |
| `Microsoft.Bot.Builder.Azure.Blobs` | `Microsoft.Agents.Storage.Blobs` |
| `Microsoft.Bot.Builder.Azure` (Cosmos DB) | `Microsoft.Agents.Storage.CosmosDb` |
| `Microsoft.Bot.Schema` | `Microsoft.Agents.Core` |
| Teams: `Microsoft.Bot.Builder.Teams` | `Microsoft.Agents.Extensions.Teams` **and** `Microsoft.Agents.Extensions.Teams.Compat` |
| `Newtonsoft.Json` | **Remove.** Use `System.Text.Json`. |

Also strip deprecated Bot Framework preview / MyGet feeds from `NuGet.config`.

### Namespaces — find/replace

| Find | Replace |
|---|---|
| `using Microsoft.Bot.Builder.Integration.AspNet.Core;` | `using Microsoft.Agents.Hosting.AspNetCore;` |
| `using Microsoft.Bot.Builder;` | `using Microsoft.Agents.Builder;` |
| `Microsoft.Bot.Builder.Dialogs` | `Microsoft.Agents.Builder.Dialogs` |
| `using Microsoft.Bot.Schema;` | `using Microsoft.Agents.Core.Models;` |
| `Microsoft.Bot.Connector.Authentication` | `Microsoft.Agents.Connector` |
| `using Microsoft.Bot.Builder.Teams;` | `using Microsoft.Agents.Extensions.Teams.Compat;` |
| `using Microsoft.Bot.Schema.Teams;` | `using Microsoft.Agents.Extensions.Teams.Models;` |
| `using Newtonsoft.Json;` | `using System.Text.Json;` |
| `using Newtonsoft.Json.Linq;` | Remove entirely |

### Types

| Old | New |
|---|---|
| `BotState` | `AgentState` |
| `BotAdapter` | `ChannelAdapter` |
| `BotFrameworkAdapter` | **Removed.** Use `CloudAdapter`. |
| `CloudAdapterBase` | `ChannelServiceAdapterBase` |
| `IBotFrameworkHttpAdapter` | `IAgentHttpAdapter` |
| `OAuthPromptSettings.ConnectionName` | `OAuthPromptSettings.AzureBotOAuthConnectionName` |
| `IAttachments.GetAttachmentInfoWithHttpMessagesAsync` | `IAttachments.GetAttachmentInfoAsync` |
| `TurnContext.TurnState.Get<T>` | `TurnContext.Services.Get<T>` |
| `JObject` / `JToken` | `JsonDocument` / `JsonElement` |

When the unqualified names are ambiguous, fully qualify:
- `Microsoft.Agents.Storage.IStorage`
- `Microsoft.Agents.Builder.IMiddleware`

### `Program.cs` — canonical rewrite

```csharp
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

// Register your agent.
builder.AddAgent<MyAgent>();

// State / storage — same shapes as BF, new namespaces.
builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddSingleton<ConversationState>();
builder.Services.AddSingleton<UserState>();

// Register any dialogs.
builder.Services.AddSingleton<MainDialog>();

// Read TokenValidation + Connections from appsettings.json.
builder.Services.AddAgentAspNetAuthentication(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/messages",
    async (HttpRequest req, HttpResponse res, IAgentHttpAdapter adapter, IAgent agent, CancellationToken ct) =>
    {
        await adapter.ProcessAsync(req, res, agent, ct);
    }).RequireAuthorization();

app.Run();
```

For an `AgentApplication`-based agent, `app.MapAgentApplicationEndpoints(requireAuth: !app.Environment.IsDevelopment())` is the shorthand for the `MapPost` line above.

**Do not** register the following legacy BF DI bindings — the new SDK wires them automatically:

- `BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication`
- `IBotFrameworkHttpAdapter` implementation
- `IBot` transient
- `ServiceClientCredentialsFactory`
- `AddControllers().AddNewtonsoftJson()`

### `appsettings.json` — two new sections

**`TokenValidation`** (inbound JWT — used by ASP.NET):

```json
"TokenValidation": {
  "Enabled": true,
  "Audiences": [ "{{MicrosoftAppId}}" ],
  "TenantId": "{{MicrosoftTenantId}}"
}
```

**`Connections` + `ConnectionsMap`** (outbound MSAL):

```json
"Connections": {
  "ServiceConnection": {
    "Settings": {
      "AuthType": "ClientSecret",
      "AuthorityEndpoint": "https://login.microsoftonline.com/{{MicrosoftTenantId}}",
      "ClientId": "{{MicrosoftAppId}}",
      "ClientSecret": "{{MicrosoftAppPassword}}",
      "Scopes": [ "https://api.botframework.com/.default" ]
    }
  }
},
"ConnectionsMap": [
  { "ServiceUrl": "*", "Connection": "ServiceConnection" }
]
```

Other `AuthType`s (managed identity, federated, certificate): see [`05-auth-and-azure-resources.md`](./05-auth-and-azure-resources.md).

**Legacy app settings** (`MicrosoftAppType`, `MicrosoftAppId`, `MicrosoftAppPassword`, `MicrosoftAppTenantId`) are harmless during migration but unused by the new SDK. Remove after validating the new blocks.

### State

- `ConversationState`, `UserState`, `PrivateConversationState` have the same API shape, new namespace (`Microsoft.Agents.Builder.State`).
- `IStatePropertyAccessor<T>` is **deprecated but functional**.
- `AutoSaveStateMiddleware` works the same way — **but do not combine it with `AgentApplication`-based agents**. Use it only with legacy `ActivityHandler`-based agents.

---

## Python

Runtime: **3.10+** (3.11+ recommended).

### Packages

| Bot Framework | Agents SDK |
|---|---|
| `botbuilder-core` | `microsoft-agents-hosting-core` |
| `botbuilder-schema` | `microsoft-agents-activity` |
| `botbuilder-azure` | `microsoft-agents-storage-blob` + `microsoft-agents-storage-cosmos` |
| `botbuilder-integration-aiohttp` | `microsoft-agents-hosting-aiohttp` |
| Teams | `microsoft-agents-hosting-teams` |
| Auth | `microsoft-agents-authentication-msal` |

### Imports — dots become underscores

> The single most common migration mistake.

| Find | Replace |
|---|---|
| `from botbuilder.core import …` | `from microsoft_agents.hosting.core import …` |
| `from botbuilder.schema import …` | `from microsoft_agents.activity import …` |
| `from botbuilder.integration.aiohttp import …` | `from microsoft_agents.hosting.aiohttp import …` |
| `from botbuilder.core.teams import …` | `from microsoft_agents.hosting.teams import …` |

### Types

| Old | New |
|---|---|
| `BotState` | `AgentState` |
| `BotFrameworkAdapter` | `CloudAdapter` |
| `BotFrameworkHttpClient` | `AgentHttpClient` |
| `OAuthPromptSettings.connection_name` | `OAuthPromptSettings.azure_bot_oauth_connection_name` |
| `turn_context.turn_state` | `turn_context.services` |

`TurnContext` static helpers moved onto `Activity` instances:

- `apply_conversation_reference()`
- `get_conversation_reference()`
- `get_reply_conversation_reference()`
- `remove_recipient_mention()`
- `get_mentions()`
- `remove_mention_text()`

### Env vars — double-underscore hierarchy

| Old | New |
|---|---|
| `MICROSOFT_APP_ID` | `CONNECTIONS__SERVICE_CONNECTION__SETTINGS__CLIENTID` |
| `MICROSOFT_APP_PASSWORD` | `CONNECTIONS__SERVICE_CONNECTION__SETTINGS__CLIENTSECRET` |
| `MICROSOFT_APP_TENANT_ID` | `CONNECTIONS__SERVICE_CONNECTION__SETTINGS__TENANTID` |

For local Emulator without credentials:

```env
CONNECTIONS__SERVICE_CONNECTION__SETTINGS__ANONYMOUS_ALLOWED=True
```

---

## Node.js / JavaScript

Runtime: **Node 20+**.

### Packages

| Bot Framework | Agents SDK |
|---|---|
| `botbuilder` | `@microsoft/agents-hosting` |
| `botframework-schema` | `@microsoft/agents-activity` |
| `botbuilder-dialogs` | `@microsoft/agents-hosting-dialogs` |
| `botbuilder-azure` | `@microsoft/agents-hosting-storage-cosmos` |
| `botbuilder-azure-blobs` | `@microsoft/agents-hosting-storage-blob` |
| (manual Express) | `@microsoft/agents-hosting-express` |
| Teams | `@microsoft/agents-hosting-extensions-teams` |

### Imports

| Old | New |
|---|---|
| `require('botframework-schema')` | `require('@microsoft/agents-activity')` |
| `require('botbuilder')` | `require('@microsoft/agents-hosting')` |
| `require('botbuilder-dialogs')` | `require('@microsoft/agents-hosting-dialogs')` |

### Env vars

| Old | New |
|---|---|
| `MicrosoftAppId` | `clientId` |
| `MicrosoftAppPassword` | `clientSecret` |
| `MicrosoftAppTenantId` | `tenantId` |

### Activity helpers

Same shift as Python — methods that were static on `TurnContext` are now instance methods on `Activity`:

- `applyConversationReference`
- `getConversationReference`
- `getReplyConversationReference`
- `removeRecipientMention`
- `getMentions`
- `removeMentionText`

### `ActivityHandler` is deprecated in JS

The JS SDK keeps `ActivityHandler` for compat, but **`AgentApplication` is the recommended new shape**. Builds in state management, removes the need for `next()` calls, supports specific event discriminators instead of the generic `onEvent`.

---

## What the diff should look like (.NET reference scenario)

For our [`samples/before-bot-framework/`](../samples/before-bot-framework/) → [`samples/after-agents-sdk/`](../samples/after-agents-sdk/) reference:

| File | Change |
|---|---|
| `*.csproj` | Replace `Microsoft.Bot.*` with `Microsoft.Agents.*`. Drop `Newtonsoft.Json`. Target `net8.0`. |
| `Program.cs` | Rewrite to minimal API + `builder.AddAgent<T>()` + `MapPost("/api/messages").RequireAuthorization()`. Delete Startup + controllers. |
| `Bots/EchoBot.cs` | Same `ActivityHandler` shape, change `using` to `Microsoft.Agents.Builder`. Or refactor to `AgentApplication`. |
| `Dialogs/UserProfileDialog.cs` | Change `using` to `Microsoft.Agents.Builder.Dialogs`. No structural change. |
| `Cards/*.json` | Unchanged. |
| `appsettings.json` | Remove `MicrosoftAppType` etc. Add `TokenValidation` + `Connections` + `ConnectionsMap`. |

## Next

→ [`04-tooling.md`](./04-tooling.md)

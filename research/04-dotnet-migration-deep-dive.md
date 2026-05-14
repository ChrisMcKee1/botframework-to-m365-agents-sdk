# .NET migration deep-dive

Source: https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-dotnet. Verified 2026-05-14.

Target runtime: **`net8.0`** (page allows `net6.0` minimum; we standardize on 8 per PRD).

## 1. Package substitutions

Replace the following NuGet packages:

| Bot Framework SDK | Microsoft 365 Agents SDK |
|---|---|
| `Microsoft.Bot.Builder.Integration.AspNet.Core` | `Microsoft.Agents.Hosting.AspNetCore` **and** `Microsoft.Agents.Authentication.Msal` |
| `Microsoft.Bot.Builder` | `Microsoft.Agents.Builder` |
| `Microsoft.Bot.Builder.Dialogs` | `Microsoft.Agents.Builder.Dialogs` |
| `Microsoft.Bot.Builder.Azure.Blobs` | `Microsoft.Agents.Storage.Blobs` |
| `Microsoft.Bot.Builder.Azure` (Cosmos DB) | `Microsoft.Agents.Storage.CosmosDb` |
| `Microsoft.Bot.Schema` | `Microsoft.Agents.Core` |

Teams users add:

- `Microsoft.Agents.Extensions.Teams` (NuGet)
- `Microsoft.Agents.Extensions.Teams.Compat` (compat shim if you previously used `Microsoft.Bot.Builder.Teams`)

Also:

- **Remove `Newtonsoft.Json` dependency.** The Agents SDK uses `System.Text.Json`. Keep Newtonsoft only if unrelated code requires it.
- **Clean NuGet.config.** Remove deprecated Bot Framework preview/MyGet feeds. Pin versions (consider `Directory.Packages.props`).

## 2. `using` rewrites

Solution-wide find/replace of exact text:

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
| `Microsoft.Bot.Builder.Teams` | `Microsoft.Agents.Extensions.Teams.Compat` |
| `Microsoft.Bot.Schema.Teams` | `Microsoft.Agents.Extensions.Teams.Models` |

## 3. Type / member renames

| Old | New |
|---|---|
| `BotState` | `AgentState` |
| `OAuthPromptSettings.ConnectionName` | `OAuthPromptSettings.AzureBotOAuthConnectionName` |
| `IAttachments.GetAttachmentInfoWithHttpMessagesAsync` | `IAttachments.GetAttachmentInfoAsync` |
| `IBotFrameworkHttpAdapter` | `IAgentHttpAdapter` |
| `BotAdapter` | `ChannelAdapter` |
| `CloudAdapterBase` | `ChannelServiceAdapterBase` |

Turn-state access changes:

| Old | New |
|---|---|
| `TurnState.Get<ConnectorClient>` | `.Services.Get<IConnectorClient>` |
| `.TurnState.Get<IUserTokenClient>` | `.Services.Get<IUserTokenClient>` |
| `.TurnState.` | `.Services.` |

## 4. `Program.cs` rewrite (canonical from MS Learn)

The Agents SDK consolidates DI in `Program.cs` (minimal API style) rather than `Startup.cs` + controller.

```csharp
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

// Register your agent.
// Dialog-based: builder.AddAgent<MyBot<MainDialog>>();
// Custom adapter (CloudAdapter subclass): builder.AddAgent<MyBot, MyAdapter>();
builder.AddAgent<MyBot>();

// Same as BF SDK — can be replaced with what you had in Startup.cs
builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddSingleton<ConversationState>();
builder.Services.AddSingleton<UserState>();

// If you use dialogs, register them
builder.Services.AddSingleton<MainDialog>();

// DO NOT register the following (legacy BF DI):
// services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
// services.AddSingleton<IBotFrameworkHttpAdapter, …>();
// services.AddTransient<IBot, …>();
// services.AddSingleton<ServiceClientCredentialsFactory>(…)
// services.AddHttpClient().AddControllers().AddNewtonsoftJson();

builder.Services.AddControllers();
builder.Services.AddAgentAspNetAuthentication(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/messages",
    async (HttpRequest request,
           HttpResponse response,
           IAgentHttpAdapter adapter,
           IAgent agent,
           CancellationToken cancellationToken) =>
    {
        await adapter.ProcessAsync(request, response, agent, cancellationToken);
    }).RequireAuthorization();

app.Run();
```

Notes:

- `AddAgentAspNetAuthentication` is the SDK extension that reads the `TokenValidation` section and wires JWT validation into ASP.NET.
- `RequireAuthorization()` on the endpoint is the line you temporarily remove when diagnosing local 401s.
- For an `AgentApplication`-based agent, you can use `app.MapAgentApplicationEndpoints(requireAuth: !app.Environment.IsDevelopment())` per the AgentApplication page.

## 5. `appsettings.json` — two new sections

### `TokenValidation`

```json
"TokenValidation": {
  "Enabled": true,
  "Audiences": [
    "{{MicrosoftAppId-value}}"
  ],
  "TenantId": "{{MicrosoftTenantId-value}}"
}
```

For all available settings see comments in [`AspNetExtensions.cs` sample](https://github.com/microsoft/Agents/blob/main/samples/dotnet/quickstart/AspNetExtensions.cs).

### `Connections` (single-tenant client secret example)

```json
"Connections": {
  "ServiceConnection": {
    "Settings": {
      "AuthType": "ClientSecret",
      "AuthorityEndpoint": "https://login.microsoftonline.com/{{MicrosoftTenantId-value}}",
      "ClientId": "{{MicrosoftAppId-value}}",
      "ClientSecret": "{{MicrosoftAppPassword-value}}",
      "Scopes": [
        "https://api.botframework.com/.default"
      ]
    }
  }
},
"ConnectionsMap": [
  { "ServiceUrl": "*", "Connection": "ServiceConnection" }
]
```

Other auth types (managed identity, federated credentials, workload identity, certificates) live in [`08-authentication-msal.md`](./08-authentication-msal.md).

### Legacy app settings — keep or remove?

`MicrosoftAppType`, `MicrosoftAppId`, `MicrosoftAppPassword`, `MicrosoftAppTenantId` are **harmless during migration but unused by the new SDK**. Remove them after the new `TokenValidation` + `Connections` blocks are verified.

## 6. Serialization changes (`System.Text.Json`)

- `JObject` / `JToken` → `JsonDocument` / `JsonElement`.
- `TeamsActivityHandler` methods that previously took `JObject` now take `JsonElement`.
- Remove `builder.Services.AddControllers().AddNewtonsoftJson();` unless something else still needs it.
- Attachment + Teams schemas moved to `Microsoft.Agents.Core.Models` and `Microsoft.Agents.Extensions.Teams.Models`.

## 7. State

- `ConversationState`, `UserState`, `PrivateConversationState` are compatible with minor differences.
- `IStatePropertyAccessor<T>` is **deprecated but functional**. Prefer the new `IAgentState` methods.
- `Dialog.RunAsync` accepts `IStatePropertyAccessor` (legacy) **or** an `AgentState` / `ConversationState` / `UserState` / `PrivateConversationState` directly. Pass the state object directly going forward.
- `AutoSaveStateMiddleware` is enhanced for auto load/save:
  ```csharp
  adapter.Use(new AutoSaveStateMiddleware(true, conversationState, userState));
  ```
  **Don't combine with `AgentApplication`-based agents** — those auto-save. Only use with legacy `ActivityHandler`-based agents.

## 8. Authentication — what changed conceptually

Bot Framework SDK validated incoming JWTs inside the adapter. **Agents SDK leaves HTTP auth to ASP.NET.**

- Copy [`AspNetExtensions.cs`](https://github.com/microsoft/Agents/blob/main/samples/dotnet/quickstart/AspNetExtensions.cs) into your project (or rely on `AddAgentAspNetAuthentication`).
- Outgoing token acquisition is done through MSAL via `Microsoft.Agents.Authentication.Msal`. See [`08-authentication-msal.md`](./08-authentication-msal.md).

## 9. Troubleshooting cheatsheet

| Symptom | Cause / fix |
|---|---|
| NuGet restore fails | Old BF preview / MyGet feeds in `NuGet.config`. Strip down to nuget.org or your approved feed. |
| `IMiddleware` / `IStorage` ambiguous reference | Fully qualify: `Microsoft.Agents.Storage.IStorage`, `Microsoft.Agents.Builder.IMiddleware`. |
| `TurnContext.TurnState` not found | Use `TurnContext.Services` and `.Services.Get<T>()`. |
| Newtonsoft errors after migration | Replace `JObject` / `JToken` with `JsonDocument` / `JsonElement`. |
| 401 from `/api/messages` locally | Either supply App ID/secret in the Emulator, **or** temporarily drop `.RequireAuthorization()` while diagnosing. |

## 10. Migration checklist (from MS Learn, verbatim shape)

- [ ] **Analyze and plan.** Identify unsupported features (Composer, LUIS/QnA). Decide migrate vs. rebuild scope.
- [ ] **Upgrade .NET target.** `net6.0` minimum, `net8.0` standard.
- [ ] **Replace packages.** Drop `Microsoft.Bot.*`. Add `Microsoft.Agents.*` (Builder, Core, Hosting.AspNetCore, Authentication.Msal, Storage providers, Teams / Teams.Compat).
- [ ] **Update namespaces and types.** Apply find/replace tables. Fix renamed APIs.
- [ ] **Rewrite `Program.cs`.** Register agent via `builder.AddAgent<T>()`, register state/storage, call `AddAgentAspNetAuthentication`, map `/api/messages` with `RequireAuthorization()`.
- [ ] **Middleware.** Prefer new turn events. Register existing `IMiddleware` via DI if you must.
- [ ] **Build and test locally.** Emulator with credentials; validate Teams behaviors.
- [ ] **Deploy and monitor.** Update Azure app settings to match new config; watch logs.

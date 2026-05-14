# Side-by-side annotated diff

A file-by-file walk through the **same scenario** in two SDKs:

- [`../before-bot-framework/`](../before-bot-framework/) — Bot Framework SDK v4 (archived; support ended Dec 31, 2025)
- [`../after-agents-sdk/`](../after-agents-sdk/) — Microsoft 365 Agents SDK

Both run on **.NET 8**, target the **same Azure Bot registration / Microsoft Entra app**, and produce the same external behavior:

1. Send an Adaptive Card welcome on conversation join.
2. Run a 2-step waterfall dialog (`TextPrompt` → `ConfirmPrompt`) to collect a name.
3. On confirm, persist the name in `UserState` and echo subsequent messages as `"[name] said: [text]"`.

This doc highlights every meaningful diff. Read it top-to-bottom.

---

## 1. The `.csproj`

| Before | After |
|---|---|
| `Microsoft.AspNetCore.Mvc.NewtonsoftJson 8.0.10` (for `AddNewtonsoftJson`) | (removed — STJ throughout) |
| `Microsoft.Bot.Builder.Integration.AspNet.Core 4.23.1` | `Microsoft.Agents.Hosting.AspNetCore 1.5.184` |
| `Microsoft.Bot.Builder.Dialogs 4.23.1` | `Microsoft.Agents.Builder.Dialogs 1.5.184` |
| — | `Microsoft.Agents.Authentication.Msal 1.5.184` (outbound auth) |
| — | `Microsoft.Agents.Extensions.Teams 1.5.184` (Teams-specific surface; optional) |
| `AdaptiveCards.Templating 2.0.6` | `AdaptiveCards.Templating 2.0.6` (unchanged) |

**Note:** `Microsoft.Agents.Hosting.AspNetCore` transitively brings in `Microsoft.Agents.Builder`, `Microsoft.Agents.Core`, and `Microsoft.Agents.Storage`, so those are not listed explicitly.

---

## 2. `Program.cs`

### Before — controllers, BF authentication, manual adapter wiring

```csharp
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddControllers().AddNewtonsoftJson();

builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddSingleton<UserState>();
builder.Services.AddSingleton<ConversationState>();
builder.Services.AddSingleton<UserProfileDialog>();
builder.Services.AddTransient<IBot, UserProfileBot>();

var app = builder.Build();
app.UseWebSockets();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### After — minimal API, `AddAgent<T>()`, MSAL + JWT extensions

```csharp
using Microsoft.Agents.Authentication.Msal;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

builder.AddAgent<UserProfileAgent>();      // ← registers IAgent + CloudAdapter + IAgentHttpAdapter

builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddSingleton<ConversationState>();
builder.Services.AddSingleton<UserState>();
builder.Services.AddSingleton<UserProfileDialog>();

builder.Services.AddDefaultMsalAuth(builder.Configuration);    // outbound (Connections section)
builder.Services.AddAgentAspNetAuthentication(builder.Configuration);  // inbound JWT (TokenValidation section)

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

var messages = app.MapPost("/api/messages",
    async (HttpRequest req, HttpResponse res, IAgentHttpAdapter adapter, IAgent agent, CancellationToken ct) =>
        await adapter.ProcessAsync(req, res, agent, ct));

if (!app.Environment.IsDevelopment())
{
    messages.RequireAuthorization();
}

app.Run();
```

### Deltas

| | Before | After |
|---|---|---|
| Routing | `[ApiController] BotController` | `app.MapPost("/api/messages", ...)` minimal API |
| Auth (inbound) | `ConfigurationBotFrameworkAuthentication` reads `MicrosoftApp*` keys | `AddAgentAspNetAuthentication(Configuration)` reads `TokenValidation` section |
| Auth (outbound) | Implicit in `ConfigurationBotFrameworkAuthentication` | Explicit `AddDefaultMsalAuth(Configuration)` reads `Connections` section |
| Adapter | `IBotFrameworkHttpAdapter` + `AdapterWithErrorHandler` subclass | `IAgentHttpAdapter` + DI'd `CloudAdapter` from `AddAgent<T>` (override via `OnTurnError`) |
| Bot type | `IBot` registered as `Transient` | `IAgent` registered by `AddAgent<T>` |
| JSON | `.AddNewtonsoftJson()` | `System.Text.Json` only |

---

## 3. `Controllers/BotController.cs`

### Before

```csharp
[ApiController]
[Route("api/messages")]
public class BotController : ControllerBase
{
    public BotController(IBotFrameworkHttpAdapter adapter, IBot bot) { ... }

    [HttpPost, HttpGet]
    public Task PostAsync() => _adapter.ProcessAsync(Request, Response, _bot);
}
```

### After

**Deleted.** The minimal-API `MapPost` in `Program.cs` replaces the controller. No `Controllers/` folder.

---

## 4. `appsettings.json`

### Before — legacy `MicrosoftApp*` keys

```json
{
  "MicrosoftAppType": "MultiTenant",
  "MicrosoftAppId": "",
  "MicrosoftAppPassword": "",
  "MicrosoftAppTenantId": ""
}
```

### After — `TokenValidation` + `Connections` + `ConnectionsMap`

```json
{
  "TokenValidation": {
    "Enabled": true,
    "Audiences": [ "{{ClientId}}" ],
    "TenantId": "{{TenantId}}"
  },
  "Connections": {
    "ServiceConnection": {
      "Settings": {
        "AuthType": "ClientSecret",
        "AuthorityEndpoint": "https://login.microsoftonline.com/{{TenantId}}",
        "ClientId": "{{ClientId}}",
        "ClientSecret": "{{ClientSecret}}",
        "Scopes": [ "https://api.botframework.com/.default" ]
      }
    }
  },
  "ConnectionsMap": [ { "ServiceUrl": "*", "Connection": "ServiceConnection" } ]
}
```

### Key changes

- `MicrosoftAppType` is **gone** — no equivalent in the Agents SDK.
- `MicrosoftAppId` → `TokenValidation.Audiences[0]` **and** `Connections.ServiceConnection.Settings.ClientId`.
- `MicrosoftAppPassword` → `Connections.ServiceConnection.Settings.ClientSecret`.
- `MicrosoftAppTenantId` → `TokenValidation.TenantId` **and** baked into `AuthorityEndpoint`.
- New `Scopes` field — explicit OAuth scope for outbound calls.
- New `ConnectionsMap` — maps which `ServiceUrl` patterns use which named connection. The wildcard `*` keeps parity with single-credential BF behavior.

See [`../../docs/05-auth-and-azure-resources.md`](../../docs/05-auth-and-azure-resources.md) for other `AuthType` values (managed identity, federated credentials, certificate).

---

## 5. `Bots/UserProfileBot.cs` → `Agents/UserProfileAgent.cs`

Same shape. Different namespaces. Different JSON.

| Concern | Before | After |
|---|---|---|
| Base class | `Microsoft.Bot.Builder.ActivityHandler` | `Microsoft.Agents.Builder.Compat.ActivityHandler` |
| Turn context | `Microsoft.Bot.Builder.ITurnContext<IMessageActivity>` | `Microsoft.Agents.Builder.ITurnContext<IMessageActivity>` |
| Message factory | `Microsoft.Bot.Builder.MessageFactory` | `Microsoft.Agents.Core.Models.MessageFactory` |
| Activity model | `Microsoft.Bot.Schema.{Attachment, ChannelAccount, IMessageActivity}` | `Microsoft.Agents.Core.Models.{Attachment, ChannelAccount, IMessageActivity}` |
| State property accessor | `Microsoft.Bot.Builder.IStatePropertyAccessor<T>` | `Microsoft.Agents.Builder.State.IStatePropertyAccessor<T>` |
| Adaptive card JSON | `Newtonsoft.Json.JsonConvert.DeserializeObject(cardJson)` | `System.Text.Json.JsonSerializer.Deserialize<JsonElement>(cardJson)` |
| Dialog runner | `dialog.RunAsync(turnContext, _dialogStateAccessor, ct)` | `dialog.RunAsync(turnContext, _conversationState, ct)` |

The **dialog runner** difference is non-cosmetic. In BF v4 you create an `IStatePropertyAccessor<DialogState>` and pass that. In the Agents SDK you pass the `ConversationState` directly and the SDK handles the dialog-state property internally.

---

## 6. `Dialogs/UserProfileDialog.cs`

Identical logic. Only the namespaces change:

| Type | Before | After |
|---|---|---|
| `ComponentDialog` | `Microsoft.Bot.Builder.Dialogs` | `Microsoft.Agents.Builder.Dialogs` |
| `WaterfallDialog`, `WaterfallStep`, `WaterfallStepContext`, `DialogTurnResult` | `Microsoft.Bot.Builder.Dialogs` | `Microsoft.Agents.Builder.Dialogs` |
| `TextPrompt`, `ConfirmPrompt`, `PromptOptions` | `Microsoft.Bot.Builder.Dialogs` | `Microsoft.Agents.Builder.Dialogs.Prompts` |
| `UserState` | `Microsoft.Bot.Builder` | `Microsoft.Agents.Builder.State` |
| `MessageFactory` | `Microsoft.Bot.Builder` | `Microsoft.Agents.Core.Models` |

That's the entire diff for the dialog file — about 6 `using` lines.

---

## 7. New file: `AspNetExtensions.cs`

The Microsoft 365 Agents SDK does **not** ship a built-in JWT bearer extension. Every Agents SDK sample copies an `AspNetExtensions.cs` (from the [official quickstart](https://github.com/microsoft/Agents/blob/main/samples/dotnet/quickstart/AspNetExtensions.cs)) into the project to wire inbound auth. We include the full file.

It exposes `IServiceCollection.AddAgentAspNetAuthentication(IConfiguration)`, which:

1. Reads the `TokenValidation` section.
2. If `Enabled` is `false` (dev), no-ops.
3. Otherwise registers `JwtBearer` with valid issuers + audiences for Azure Bot Service and Entra ID tokens.

---

## 8. What this sample does NOT show

The Agents SDK has a more modern shape than `ActivityHandler` + `ComponentDialog`. We kept the legacy shape so the diff focuses on **package / namespace / config / hosting** — the minimum changes to make the migration mechanical.

Things to consider next:

| Modernization | What it replaces | Why later |
|---|---|---|
| **`AgentApplication`** with `OnActivity` route-table style handlers | `ActivityHandler` overrides | Cleaner agent definition. Read [`../../docs/02-agents-sdk-overview.md`](../../docs/02-agents-sdk-overview.md) §"Core abstractions". |
| **`AgentState.GetValueAsync` / `SetValueAsync`** | `CreateProperty<T>` + `IStatePropertyAccessor<T>` | The legacy `CreateProperty` API is marked `[Obsolete]` (you'll see CS0618 warnings on build). |
| **Managed identity** (`AuthType: SystemManagedIdentity` or `UserManagedIdentity`) | `AuthType: ClientSecret` | No secrets to rotate. Pick up after initial migration. See [`../../docs/05-auth-and-azure-resources.md`](../../docs/05-auth-and-azure-resources.md). |
| **Blob / Cosmos storage** | `MemoryStorage` | Required for multi-instance hosting. Drop-in replacement via `IStorage`. |

---

## 9. What stayed the same

- Azure Bot registration (same App ID, same channel configuration)
- Microsoft Entra app registration (same client ID / tenant)
- Conversation / user state semantics
- Dialog programming model (waterfall, prompts, accessors)
- Adaptive Card JSON
- ASP.NET Core hosting (same `WebApplication` builder, same `appsettings.json` mechanics)
- Same external behavior in Teams, Web Chat, M365 Agents Playground, etc.

That's the point of the side-by-side. **Most code stays.** The migration is mechanical edits to packages, namespaces, hosting, and config.

# `after-agents-sdk` — Microsoft 365 Agents SDK reference

The **modernized** side of the side-by-side. Same scenario as [`../before-bot-framework/`](../before-bot-framework/), migrated to the Microsoft 365 Agents SDK.

## What's the same

- Same external behavior: welcome adaptive card on join, waterfall dialog to collect a name with confirm, echo with stored name afterward
- Same shape: `ActivityHandler` + `ConversationState`/`UserState` + `ComponentDialog` + waterfall + `TextPrompt` + `ConfirmPrompt`
- Same Azure Bot registration and App ID work — no resource changes required

## What changed

| Concern | Before (BF v4) | After (Agents SDK) |
|---|---|---|
| Packages | `Microsoft.Bot.Builder.*` | `Microsoft.Agents.*` |
| Hosting | `Program.cs` + `BotController` + `IBotFrameworkHttpAdapter` | Minimal API + `builder.AddAgent<T>()` + `IAgentHttpAdapter` |
| Adapter | `CloudAdapter` registered manually | Registered by `AddAgent<T>()` (still `CloudAdapter` under the hood) |
| Inbound auth | `ConfigurationBotFrameworkAuthentication` (read `MicrosoftApp*`) | `AddAgentAspNetAuthentication` (JWT, read `TokenValidation`) |
| Outbound auth | Adapter handles via `MicrosoftAppCredentials` | `AddDefaultMsalAuth` (read `Connections` + `ConnectionsMap`) |
| Config keys | `MicrosoftAppType` / `MicrosoftAppId` / `MicrosoftAppPassword` / `MicrosoftAppTenantId` | `TokenValidation.{Audiences,TenantId}` + `Connections.ServiceConnection.Settings.{AuthType,ClientId,ClientSecret,Scopes}` + `ConnectionsMap` |
| JSON | Newtonsoft (`JsonConvert`, `JObject`) | `System.Text.Json` (`JsonSerializer`, `JsonElement`) |
| Dialog run | `dialog.RunAsync(turnContext, IStatePropertyAccessor<DialogState>, ct)` | `dialog.RunAsync(turnContext, ConversationState, ct)` |
| Namespaces | `Microsoft.Bot.Builder` / `Microsoft.Bot.Schema` / `Microsoft.Bot.Builder.Dialogs` | `Microsoft.Agents.Builder.Compat` / `Microsoft.Agents.Core.Models` / `Microsoft.Agents.Builder.Dialogs` |

`AspNetExtensions.cs` is a **sample-local helper** — every Agents SDK quickstart copies it. The SDK does not ship a built-in JWT-bearer wiring.

See [`../side-by-side/`](../side-by-side/) for the file-by-file annotated diff.

## Layout

```
after-agents-sdk/
├── MigrationSample.After.csproj   ← Microsoft.Agents.* packages
├── Program.cs                   ← Minimal API; builder.AddAgent<T>()
├── AspNetExtensions.cs          ← Sample-local JWT bearer wiring
├── Agents/
│   └── UserProfileAgent.cs      ← ActivityHandler (Compat namespace)
├── Dialogs/
│   └── UserProfileDialog.cs     ← ComponentDialog (waterfall) — same shape
├── Models/
│   └── UserProfile.cs
├── Cards/
│   └── welcomeCard.json
├── appsettings.json             ← TokenValidation + Connections + ConnectionsMap
└── appsettings.Development.json ← TokenValidation.Enabled = false
```

## Run locally (Microsoft 365 Agents Playground)

Prerequisites: **.NET 8 SDK** (or 9 / 10 with rollforward), **Microsoft 365 Agents Playground**.

Install Agents Playground globally (one time):

```pwsh
npm install -g @microsoft/teams-app-test-tool
```

Run the sample:

```pwsh
dotnet build
dotnet run
```

The agent listens on `http://localhost:5001/api/messages` (pinned in [Properties/launchSettings.json](Properties/launchSettings.json) so it can run alongside the before sample on 5000). With `ASPNETCORE_ENVIRONMENT=Development` (set by the launch profile), `RequireAuthorization()` is **skipped** so the Playground can send anonymous requests.

Start the Playground in another terminal (pointed at the agent's endpoint):

```pwsh
teamsapptester start --bot http://localhost:5001/api/messages
```

The Playground opens in your browser. Send any message to start the conversation.

## Run for real (deployed)

1. Set `TokenValidation.Audiences` to your Azure Bot's Microsoft App ID (a GUID).
2. Set `TokenValidation.TenantId` to your home tenant.
3. Fill `Connections.ServiceConnection.Settings.ClientId` / `ClientSecret` / `AuthorityEndpoint` for outbound calls back to the channel.
4. Deploy. Inbound `/api/messages` requires a valid Azure Bot Service / Entra JWT.

For Government clouds, set `TokenValidation.IsGov = true` and adjust scopes per [`../../docs/05-auth-and-azure-resources.md`](../../docs/05-auth-and-azure-resources.md).

## Run end-to-end in Microsoft Teams

For the full Azure Bot + dev tunnel + Teams sideload walkthrough, see [`../../docs/09-running-in-teams.md`](../../docs/09-running-in-teams.md). The same Teams app package is reused by the before sample, so you can stop one, start the other, and compare behavior in the same chat.

## Where this came from

See [`../../docs/08-migration-playbook.md`](../../docs/08-migration-playbook.md) for the step-by-step migration recipe.

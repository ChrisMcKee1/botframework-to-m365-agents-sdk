# `before-bot-framework` — Bot Framework SDK v4 reference

The **legacy** side of the side-by-side. Reflects what a typical pro-code Bot Framework v4 codebase looks like today.

> ⚠ Bot Framework SDK is archived. Support tickets ended **Dec 31, 2025**. This sample builds and runs, but the framework receives no further patches.

## Scenario

`UserProfileBot` — exercises everything a typical BF bot has:

- `ActivityHandler` overriding `OnMessageActivityAsync` and `OnMembersAddedAsync`
- `ConversationState` + `UserState` with property accessors
- A waterfall dialog (`UserProfileDialog`) that collects a name with confirm
- Adaptive card welcome message (`Cards/welcomeCard.json`)
- `BotController` controller at `/api/messages`

After the user confirms a name, subsequent messages echo back: `"[name] said: [text]"`.

## Layout

```
before-bot-framework/
├── MigrationSample.Before.csproj   ← Microsoft.Bot.Builder.* packages
├── Program.cs                  ← Startup-style DI; controllers
├── Controllers/
│   └── BotController.cs        ← POST /api/messages
├── Bots/
│   ├── AdapterWithErrorHandler.cs   ← CloudAdapter subclass with OnTurnError
│   └── UserProfileBot.cs            ← ActivityHandler
├── Dialogs/
│   └── UserProfileDialog.cs    ← ComponentDialog (waterfall)
├── Models/
│   └── UserProfile.cs
├── Cards/
│   └── welcomeCard.json        ← Adaptive Card template
├── appsettings.json            ← MicrosoftAppType / Id / Password / TenantId
└── appsettings.Development.json
```

## Run locally

Prerequisites: **.NET 8 SDK** (or 9 / 10 with rollforward), **Bot Framework Emulator**.

```pwsh
dotnet build
dotnet run
```

The bot listens on `http://localhost:5000/api/messages` (pinned in [Properties/launchSettings.json](Properties/launchSettings.json) so it doesn't collide with the after sample on 5001).

In the Bot Framework Emulator:
1. **Open Bot**
2. Bot URL: `http://localhost:5000/api/messages`
3. Leave Microsoft App ID / Password blank for anonymous local debug.
4. Send any message to start the conversation.

## Required Azure resources (when deploying)

- Azure Bot registration
- Microsoft Entra app registration (App ID + secret)
- Hosting (App Service / Functions / etc.)
- Fill `MicrosoftAppType`, `MicrosoftAppId`, `MicrosoftAppPassword`, `MicrosoftAppTenantId` in `appsettings.json`.

## Run end-to-end in Microsoft Teams

For the full Azure Bot + dev tunnel + Teams sideload walkthrough, see [`../../docs/09-running-in-teams.md`](../../docs/09-running-in-teams.md). The same Teams app package is reused by the after sample, so you can stop one, start the other, and compare behavior in the same chat.

## Where this is going

The same scenario, migrated to the Microsoft 365 Agents SDK, is in [`../after-agents-sdk/`](../after-agents-sdk/). The diff is documented in [`../side-by-side/`](../side-by-side/).

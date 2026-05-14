# 08 — Migration playbook

End-to-end checklist a developer follows. Phases mirror the MS Learn migration guidance: **analyze → upgrade target → replace packages → update namespaces → rewrite startup → middleware/state → build/test → deploy/monitor**.

Read alongside the canonical mapping in [`03-migration-mapping.md`](./03-migration-mapping.md).

---

## Phase 0 — Plan

- [ ] Complete the [discovery checklist](./07-discovery-checklist.md).
- [ ] Identify **what doesn't migrate** (Composer / Adaptive Dialogs / LG / LUIS / QnA / App Insights helpers / Streaming Connections) — see [`01-bot-framework-overview.md` § What goes away](./01-bot-framework-overview.md).
- [ ] Decide scope: **lift-and-shift only** vs. **lift-and-shift + AI replacement** (Foundry / Microsoft Agent Framework).
- [ ] Inventory Azure resources to preserve (Azure Bot, App ID, channels, hosting).
- [ ] Branch the existing bot repo: `migration/agents-sdk`.

---

## Phase 1 — Upgrade the runtime

### .NET
- [ ] Update `TargetFramework` to `net8.0`.
- [ ] Update `global.json` if pinned.
- [ ] Run `dotnet restore` on the legacy code to confirm baseline still builds.

### Python
- [ ] Bump runtime to **≥ 3.10** (3.11+ recommended).
- [ ] Update `pyproject.toml` / `setup.py` / runtime stamp on hosting platform.

### Node
- [ ] Bump `engines.node` to `>=20.0.0`.
- [ ] Update hosting runtime stamp.

---

## Phase 2 — Replace packages

Use the tables in [`03-migration-mapping.md`](./03-migration-mapping.md).

### .NET
- [ ] Remove `Microsoft.Bot.*` packages from `.csproj`.
- [ ] Add `Microsoft.Agents.Builder`, `Microsoft.Agents.Core`, `Microsoft.Agents.Hosting.AspNetCore`, `Microsoft.Agents.Authentication.Msal`.
- [ ] Add storage providers: `Microsoft.Agents.Storage.Blobs` and/or `Microsoft.Agents.Storage.CosmosDb`.
- [ ] Add `Microsoft.Agents.Builder.Dialogs` if using dialogs.
- [ ] Add Teams: `Microsoft.Agents.Extensions.Teams` (and `.Compat` if you used `Microsoft.Bot.Builder.Teams`).
- [ ] Remove `Newtonsoft.Json` unless other code needs it.
- [ ] Clean `NuGet.config` — strip BF preview / MyGet feeds.

### Python
- [ ] Update `requirements.txt`: drop `botbuilder-*`, add `microsoft-agents-*`.
- [ ] Add `black` and `flake8` to dev deps.

### Node
- [ ] Update `package.json`: drop `botbuilder*`, add `@microsoft/agents-*`.
- [ ] `npm install` (or `pnpm` / `yarn`).

---

## Phase 3 — Update namespaces and types

### .NET — find/replace
- [ ] `using Microsoft.Bot.Builder.Integration.AspNet.Core;` → `using Microsoft.Agents.Hosting.AspNetCore;`
- [ ] `using Microsoft.Bot.Builder;` → `using Microsoft.Agents.Builder;`
- [ ] `using Microsoft.Bot.Schema;` → `using Microsoft.Agents.Core.Models;`
- [ ] `Microsoft.Bot.Connector.Authentication` → `Microsoft.Agents.Connector`
- [ ] `using Microsoft.Bot.Builder.Teams;` → `using Microsoft.Agents.Extensions.Teams.Compat;`
- [ ] `using Microsoft.Bot.Schema.Teams;` → `using Microsoft.Agents.Extensions.Teams.Models;`
- [ ] `using Newtonsoft.Json;` → `using System.Text.Json;`
- [ ] Remove `using Newtonsoft.Json.Linq;`
- [ ] Fix renamed types: `BotState` → `AgentState`, `BotAdapter` → `ChannelAdapter`, `IBotFrameworkHttpAdapter` → `IAgentHttpAdapter`, `BotFrameworkAdapter` → `CloudAdapter`.
- [ ] Fix renamed members: `OAuthPromptSettings.ConnectionName` → `.AzureBotOAuthConnectionName`, `TurnContext.TurnState` → `TurnContext.Services`, `IAttachments.GetAttachmentInfoWithHttpMessagesAsync` → `IAttachments.GetAttachmentInfoAsync`.
- [ ] Replace `JObject` / `JToken` with `System.Text.Json` `JsonDocument` / `JsonElement`.

### Python — find/replace
- [ ] **Dots → underscores:** `from botbuilder.core` → `from microsoft_agents.hosting.core` (and the other module paths).
- [ ] `BotState` → `AgentState`, `BotFrameworkAdapter` → `CloudAdapter`.
- [ ] Move `TurnContext.apply_conversation_reference()` and siblings onto `activity.…`.
- [ ] `turn_context.turn_state` → `turn_context.services`.

### Node — find/replace
- [ ] `require('botbuilder')` → `require('@microsoft/agents-hosting')`.
- [ ] `require('botframework-schema')` → `require('@microsoft/agents-activity')`.
- [ ] `require('botbuilder-dialogs')` → `require('@microsoft/agents-hosting-dialogs')`.
- [ ] Move `TurnContext.applyConversationReference` and siblings onto `activity.…`.
- [ ] Decide: stay on `ActivityHandler` (compat, deprecated) or refactor to `AgentApplication`.

---

## Phase 4 — Rewrite startup / hosting

### .NET — replace `Startup.cs` + controllers with minimal API + DI

- [ ] Delete `Startup.cs` if present.
- [ ] Delete `Controllers/BotController.cs` if present.
- [ ] Rewrite `Program.cs` per the [canonical pattern](./03-migration-mapping.md#programcs--canonical-rewrite):
  - [ ] `builder.AddAgent<MyAgent>();`
  - [ ] Register state + storage (`MemoryStorage`, `ConversationState`, `UserState`, dialogs).
  - [ ] `builder.Services.AddAgentAspNetAuthentication(builder.Configuration);`
  - [ ] `app.UseAuthentication(); app.UseAuthorization();`
  - [ ] `app.MapPost("/api/messages", …).RequireAuthorization();` *(or `MapAgentApplicationEndpoints`)*
- [ ] **Do not** register the legacy DI bindings: `BotFrameworkAuthentication`, `IBotFrameworkHttpAdapter`, `IBot` transient, `ServiceClientCredentialsFactory`, `AddNewtonsoftJson`.

### Python — replace aiohttp + manual adapter with `CloudAdapter` + `MsalConnectionManager` + `AgentApplication`

- [ ] Use `load_configuration_from_env(os.environ)` to read the new double-underscore env vars.
- [ ] Construct `MsalConnectionManager(**agents_sdk_config)`.
- [ ] Construct `CloudAdapter(connection_manager=...)`.
- [ ] Build `AgentApplication[TurnState](storage=..., adapter=..., authorization=...)`.
- [ ] Register handlers via decorators (`@AGENT_APP.activity("message")` etc.).

### Node — choose one of two server setups

- [ ] **Option A (minimal):** `startServer(new MyAgent())` from `@microsoft/agents-hosting-express`.
- [ ] **Option B (manual Express):** `loadAuthConfigFromEnv` + `authorizeJWT` middleware + `server.post('/api/messages', ...)`.

---

## Phase 5 — Update configuration

### .NET — `appsettings.json`
- [ ] Add `TokenValidation` block (audiences, tenant ID).
- [ ] Add `Connections.ServiceConnection.Settings` block (AuthType, ClientId, ClientSecret, AuthorityEndpoint, Scopes).
- [ ] Add `ConnectionsMap` block (`ServiceUrl: "*"` → `Connection: "ServiceConnection"`).
- [ ] **Keep** legacy `MicrosoftApp*` settings temporarily — remove only after the new config is validated end-to-end.
- [ ] (Optional) Add `MSALConfiguration` knobs.

### Python — env vars
- [ ] `CONNECTIONS__SERVICE_CONNECTION__SETTINGS__CLIENTID`
- [ ] `CONNECTIONS__SERVICE_CONNECTION__SETTINGS__CLIENTSECRET`
- [ ] `CONNECTIONS__SERVICE_CONNECTION__SETTINGS__TENANTID`
- [ ] (Local) `CONNECTIONS__SERVICE_CONNECTION__SETTINGS__ANONYMOUS_ALLOWED=True`

### Node — env vars
- [ ] `clientId`, `clientSecret`, `tenantId` (lowercase).

---

## Phase 6 — Middleware and state

- [ ] Existing `IMiddleware` implementations: register via DI; verify they still attach to the adapter.
- [ ] Prefer new lifecycle hooks on `AgentApplication`: `OnBeforeTurn`, `OnAfterTurn`, `OnTurnError`.
- [ ] If using `AutoSaveStateMiddleware`, ensure agent is **`ActivityHandler`-based, not `AgentApplication`-based** (the latter auto-saves; double-saving causes issues).
- [ ] Verify `ConversationState` / `UserState` accessors compile (`IStatePropertyAccessor<T>` is deprecated but functional).

---

## Phase 7 — Build, test, and run locally

- [ ] `dotnet build` / `npm run build` / `python -m py_compile` clean.
- [ ] Run against **Bot Framework Emulator** (legacy) or **Microsoft 365 Agents Playground** (preferred).
- [ ] Exercise scenarios from your existing test suite — particularly anything that touches:
  - Adaptive Cards
  - Teams-specific behaviors (mentions, channel data, invokes)
  - OAuth prompts
  - Custom dialogs
  - Custom middleware
- [ ] Fix unit test imports / namespaces.

---

## Phase 8 — Deploy to a non-production environment

- [ ] Stage new app settings (`TokenValidation`, `Connections`, `ConnectionsMap`) in the target App Service / Functions / AKS environment.
- [ ] Deploy the migrated bot to a **dev or staging slot bound to the same Azure Bot registration** as production (or a parallel Azure Bot pointing to the same App ID, depending on your slot strategy).
- [ ] Smoke test on each in-use channel.
- [ ] Monitor logs for MSAL token issues; raise `Microsoft.Agents.Authentication.Msal` to `Trace` temporarily if needed.

---

## Phase 9 — Cut over

- [ ] Swap slot or repoint Azure Bot endpoint to the new deployment.
- [ ] **Keep the old build deployable for fast rollback.**
- [ ] Validate channels live.
- [ ] Watch metrics for 24–48 hours.

---

## Phase 10 — Cleanup

- [ ] Remove legacy `MicrosoftAppType`, `MicrosoftAppId`, `MicrosoftAppPassword`, `MicrosoftAppTenantId` settings (now unused).
- [ ] Remove archived BF SDK preview / MyGet feeds from CI.
- [ ] Update README, runbooks, and on-call docs.
- [ ] Plan phase-2 work: managed identity, Foundry / Microsoft Agent Framework for LUIS+QnA replacement, M365 Copilot publish.

---

## Reference

- Canonical mapping table: [`03-migration-mapping.md`](./03-migration-mapping.md)
- Sample diff: [`samples/side-by-side/`](../samples/side-by-side/)
- Sources: [`../research/sources.md`](../research/sources.md)

# 07 — Discovery checklist (scoping a migration)

**Work through this list before sizing the migration.** Each yes/no answer sizes part of the work.

Distribute this checklist to stakeholders ahead of any planning meeting so answers are ready.

---

## A. Runtime & hosting

### A1. Language and runtime
- [ ] **C# / .NET** — version: `_______` (target .NET 8; minimum .NET 6)
- [ ] **JavaScript / TypeScript** — Node version: `_______` (target Node 20+)
- [ ] **Python** — version: `_______` (target 3.10+)
- [ ] **Java** — **⚠ NOT on the Agents SDK roadmap.** No first-party migration path. Record this risk if present.

### A2. Hosting
- [ ] App Service
- [ ] Azure Functions
- [ ] AKS
- [ ] On-prem / VM
- [ ] Other: `_______`

Hosting drives the recommended `AuthType` in the `Connections` block — see [`05-auth-and-azure-resources.md`](./05-auth-and-azure-resources.md).

### A3. Region & cloud
- [ ] Microsoft commercial cloud (default)
- [ ] **Microsoft 365 Government** — **⚠ publishing via Microsoft 365 Agents Toolkit is not supported in M365 Government tenants.** Plan for this risk.

---

## B. Channels

### B1. Channels currently in use
- [ ] Microsoft Teams
- [ ] Web Chat / DirectLine
- [ ] Slack
- [ ] Facebook Messenger
- [ ] Email
- [ ] SMS
- [ ] Other: `_______`

### B2. New channels of interest post-migration
- [ ] **Microsoft 365 Copilot** (first-class in Agents SDK — major new surface area)
- [ ] Anything else

### B3. Teams app manifest version (if Teams)
Current: `_______`. Must be **≥ 1.21** to surface as a custom engine agent in M365 Copilot / new Teams.

### B4. M365 Copilot caveats acknowledged
- [ ] Streaming responses required
- [ ] Typing activities **not supported**
- [ ] Rich-card support limited

---

## C. Legacy features that DO NOT migrate

Each item below requires refactor / rebuild, not a package swap. If any are present, scope refactor budget.

### C1. Authoring tools
- [ ] Bot Framework **Composer**?
- [ ] **Adaptive Dialogs**?
- [ ] **Language Generation (LG)** templates?
- [ ] **AdaptiveExpressions** in code?

→ If yes, those flows must be rewritten as code (waterfall dialogs, AI orchestration) or as Copilot Studio assets.

### C2. AI services
- [ ] **LUIS** intents / entities? — replace with Azure OpenAI + function calling
- [ ] **QnA Maker** knowledge bases? — replace with Azure AI Search + RAG or Foundry knowledge sources
- [ ] **Orchestrator** (legacy multi-model router)? — replace with Foundry orchestration or Microsoft Agent Framework workflows

→ See [`06-ai-orchestration-options.md`](./06-ai-orchestration-options.md). Drives whether stretch sample (Foundry / Microsoft Agent Framework) is in scope.

### C3. Telemetry
- [ ] Custom `IBotTelemetryClient` / App Insights bot-telemetry helpers? — replace with standard OpenTelemetry / `ILogger` patterns

### C4. Other deprecated surfaces
- [ ] **Inspection middleware** for debugging?
- [ ] **Streaming Connections** (legacy)?
- [ ] **`QueueStorage`** (BotBuilder)?
- [ ] **`TemplateManager`**?
- [ ] **Payments** or other deprecated activity types?
- [ ] **Bot Framework CLI** (`bf`) in build / CI scripts?
- [ ] **Yeoman generators** in build / CI scripts?

---

## D. What DOES migrate (package swap, possibly small code edits)

### D1. Activity handling
- [ ] `ActivityHandler` / `TeamsActivityHandler` subclass? — same shape, new namespace. Optional refactor to `AgentApplication`.

### D2. Dialogs
- [ ] **Waterfall dialogs**? — supported, namespace swap.
- [ ] **Component dialogs**? — supported, namespace swap.
- [ ] **Custom dialog types**? — likely supported; review case-by-case.

### D3. Custom middleware
- [ ] Custom `IMiddleware` implementations? — register via DI; or refactor to `OnBeforeTurn` / `OnAfterTurn` / `OnTurnError` lifecycle hooks.

### D4. State storage
- [ ] **In-memory** — fine for dev
- [ ] **Azure Blob** — package swap to `Microsoft.Agents.Storage.Blobs`
- [ ] **Azure Cosmos DB** — package swap to `Microsoft.Agents.Storage.CosmosDb`
- [ ] **Custom `IStorage`** — implement against new interface

### D5. Adaptive Cards
- [ ] Adaptive Cards in use? — unchanged. Cards are channel features, not SDK features.

### D6. OAuth / SSO
- [ ] User-token connections / `OAuthPrompt`? — supported. Note rename: `OAuthPromptSettings.ConnectionName` → `OAuthPromptSettings.AzureBotOAuthConnectionName`.

---

## E. Authentication

### E1. Current auth model
- [ ] Single-tenant App ID + client secret
- [ ] Multi-tenant App ID + client secret
- [ ] User-assigned managed identity
- [ ] System-assigned managed identity
- [ ] Federated credentials / Workload Identity Federation
- [ ] Certificate (thumbprint)
- [ ] Certificate (subject name + SN+I)

Drives the `Connections.ServiceConnection.Settings.AuthType` block. Recommendation for first pass: **stay on existing auth model** to minimize migration risk, modernize to MI later.

### E2. App ID / secret / tenant ID inventory
- [ ] Inventory documented? `_______`
- [ ] Stored in Key Vault? `_______`
- [ ] Secret rotation schedule? `_______`

---

## F. Build, test, CI/CD

### F1. Build / package management
- [ ] **.NET:** Bot Framework MyGet / preview feeds in `NuGet.config`? — must remove
- [ ] **Node:** `botbuilder*` packages in `package.json`? — must replace
- [ ] **Python:** `botbuilder-*` in `requirements.txt`? — must replace
- [ ] **Newtonsoft.Json** in `.csproj`? — drop unless other code needs it

### F2. Test surface
- [ ] Unit tests for activity handlers / dialogs? — keep, may need small import updates
- [ ] Emulator-driven smoke tests? — migrate to **Agents Playground**
- [ ] Integration tests? `_______`

### F3. Pipelines
- [ ] CI builds bot project today? — yes / no
- [ ] CI uses Bot Framework CLI / templates? — must remove
- [ ] Deploy target: App Service / Functions / AKS / other? `_______`

---

## G. Timeline & business context

### G1. Deadlines
- [ ] Internal deadline for migration? `_______`
- [ ] Regulatory deadline? `_______`
- [ ] **Note:** Bot Framework SDK support tickets ended **Dec 31, 2025**. There is no Microsoft-imposed runtime deadline — bots keep running.

### G2. Stretch scope
- [ ] Interested in surfacing in **M365 Copilot** as a new channel?
- [ ] Interested in **Foundry / Microsoft Agent Framework** AI variant (replaces LUIS / QnA)?
- [ ] Interested in any **multi-channel** expansion (Web, SMS, Email)?

### G3. Migration capacity
- [ ] Engineers available for migration? `_______`
- [ ] Need external support (FastTrack, support, partners)? `_______`

---

## H. Output of this checklist

Fill these out once the checklist is complete:

- **Sized scope:** lift-and-shift / lift-and-shift + retire LUIS+QnA / lift-and-shift + new channels / full re-architect
- **Azure resources to keep:** ____________________
- **App settings to add:** `TokenValidation`, `Connections`, `ConnectionsMap`
- **App settings to remove (after validation):** `MicrosoftAppType`, `MicrosoftAppId`, `MicrosoftAppPassword`, `MicrosoftAppTenantId`
- **Refactor items (no direct migration):** ____________________
- **Owner:** ____________________
- **Target migration window:** ____________________

## Next

→ [`08-migration-playbook.md`](./08-migration-playbook.md)

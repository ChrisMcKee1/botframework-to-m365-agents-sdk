# 09 — Running the samples in Microsoft Teams

> The point of this doc: provision **one** Azure Bot + **one** Teams app once, then **swap which sample is running** to see the migration diff in a real Teams chat. Same registration, same Teams package, two SDKs.

This walkthrough was written and validated end-to-end against:

- Azure CLI `2.x` signed in to a Microsoft 365 sandbox tenant
- Dev Tunnels CLI `1.0.1824+`
- .NET `8` SDK (samples target `net8.0` — newer SDKs work via rollforward)
- Teams (developer-tenant sideload, no admin approval needed)

> **Heads up on auth model.** `az bot create` no longer accepts `--app-type MultiTenant`; multi-tenant bot creation is deprecated. We use `SingleTenant` here. That's fine for sideloading in a sandbox/dev tenant. Cross-tenant scenarios need `UserAssignedMSI` with a federated identity per channel.

---

## What you get

```
Microsoft Teams (sandbox tenant)
  └─ Sideloaded "Agents Migration Demo" app  ─┐
                                              │  (msteams channel)
Azure Bot Service                             │
  └─ agents-mig-demo-XXXX                     │
       msaAppType: SingleTenant               │
       endpoint:  https://<tunnel-N>/api/messages ─── relays through ───┐
                                                                      │
Dev tunnel (https → http, two ports on one tunnel)                    │
  ├─ <id>-5000.use.devtunnels.ms  → before sample                    │
  └─ <id>-5001.use.devtunnels.ms  → after sample                     │
                                                                      ▼
Localhost (both can run at the same time)
  ├─ samples/before-bot-framework  → http://localhost:5000  (BF v4)
  └─ samples/after-agents-sdk     → http://localhost:5001  (Agents SDK)
```

You can keep both samples running simultaneously and switch which one Teams talks to by re-pointing the Azure Bot endpoint between the two tunnel URLs.

---

## Prerequisites

- Azure subscription you can create resources in. A Microsoft 365 sandbox tenant is fine — see the [Microsoft 365 Developer Program](https://developer.microsoft.com/microsoft-365/dev-program).
- Azure CLI `az` (`az login`).
- [Dev Tunnels CLI](https://learn.microsoft.com/azure/developer/dev-tunnels/get-started) (`devtunnel user login`).
- .NET `8`+ SDK.
- (Optional) [Bot Framework Emulator](https://learn.microsoft.com/azure/bot-service/bot-service-debug-emulator) — for local-only smoke tests without going through Azure.
- (Optional) [Microsoft 365 Agents Playground](https://www.npmjs.com/package/@microsoft/teams-app-test-tool) — same idea, for the after sample.

> The before sample also runs entirely locally in **Bot Framework Emulator** with no Azure resources at all — just `dotnet run` and point the Emulator at `http://localhost:5000/api/messages`. The same anonymous-local path works for the after sample on `http://localhost:5001/api/messages` (Emulator or Microsoft 365 Agents Playground). Skip the rest of this doc if you only need that loop.

---

## 1. Provision Azure resources (one-time)

Run these in PowerShell from anywhere. Adjust `$LOCATION` if you want; `global` is required for the Azure Bot itself.

```pwsh
$suffix      = -join ((48..57) | Get-Random -Count 4 | ForEach-Object {[char]$_})
$env:DEMO_NAME      = "agents-mig-demo-$suffix"
$env:DEMO_RG        = "rg-$($env:DEMO_NAME)"
$env:DEMO_APPNAME   = "$($env:DEMO_NAME)-app"
$env:DEMO_LOCATION  = 'eastus'

# Resource group
az group create -n $env:DEMO_RG -l $env:DEMO_LOCATION -o jsonc

# Entra app registration (single-tenant — required for current az bot create)
$env:DEMO_APPID = az ad app create `
    --display-name $env:DEMO_APPNAME `
    --sign-in-audience AzureADMyOrg `
    --query appId -o tsv
az ad sp create --id $env:DEMO_APPID -o none

# Capture tenant + secret. Treat $env:DEMO_SECRET like a password.
$env:DEMO_TENANTID = az account show --query tenantId -o tsv
$env:DEMO_SECRET = az ad app credential reset `
    --id $env:DEMO_APPID `
    --display-name 'bot-secret' `
    --years 1 `
    --query password -o tsv

# Make sure the Bot Service provider is registered, then create the bot
az provider register -n Microsoft.BotService --wait
az bot create `
    --resource-group $env:DEMO_RG `
    --name $env:DEMO_NAME `
    --app-type SingleTenant `
    --appid $env:DEMO_APPID `
    --tenant-id $env:DEMO_TENANTID `
    --sku F0 -l global -o jsonc

# Add the Microsoft Teams channel
az bot msteams create -g $env:DEMO_RG -n $env:DEMO_NAME -o none
```

When you're done you should have:

- An Azure resource group
- An Entra application + service principal
- An `azurebot` resource (`SingleTenant`, F0, in the `global` region) with the Microsoft Teams channel enabled

> **Save these values.** You'll need `$env:DEMO_APPID`, `$env:DEMO_TENANTID`, `$env:DEMO_SECRET` for the next step. The secret is only printed once — if you close the shell, rotate it with `az ad app credential reset --id $env:DEMO_APPID --append --display-name 'rotated' --years 1 --query password -o tsv`.

---

## 2. Wire credentials into both samples

Use **`dotnet user-secrets`** so credentials never land in `appsettings.json`. Both projects already declare a `UserSecretsId` after running these commands once.

```pwsh
# Init user-secrets (idempotent — safe to re-run)
dotnet user-secrets init --project samples\before-bot-framework
dotnet user-secrets init --project samples\after-agents-sdk

# --- Before sample (Bot Framework v4 reads MicrosoftApp* directly) ---
cd samples\before-bot-framework
dotnet user-secrets set "MicrosoftAppType"     "SingleTenant"
dotnet user-secrets set "MicrosoftAppId"       $env:DEMO_APPID
dotnet user-secrets set "MicrosoftAppPassword" $env:DEMO_SECRET
dotnet user-secrets set "MicrosoftAppTenantId" $env:DEMO_TENANTID

# --- After sample (Agents SDK uses TokenValidation + Connections) ---
cd ..\after-agents-sdk
dotnet user-secrets set "TokenValidation:Enabled"      "true"
dotnet user-secrets set "TokenValidation:Audiences:0"  $env:DEMO_APPID
dotnet user-secrets set "TokenValidation:TenantId"     $env:DEMO_TENANTID
dotnet user-secrets set "Connections:ServiceConnection:Settings:AuthType"          "ClientSecret"
dotnet user-secrets set "Connections:ServiceConnection:Settings:AuthorityEndpoint" "https://login.microsoftonline.com/$env:DEMO_TENANTID"
dotnet user-secrets set "Connections:ServiceConnection:Settings:ClientId"          $env:DEMO_APPID
dotnet user-secrets set "Connections:ServiceConnection:Settings:ClientSecret"      $env:DEMO_SECRET
dotnet user-secrets set "Connections:ServiceConnection:Settings:Scopes:0"          "https://api.botframework.com/.default"
```

Two things to remember about user-secrets:

1. They only load when `ASPNETCORE_ENVIRONMENT=Development`. The Visual Studio launch profile and `dotnet run` set this by default, so you'll usually be fine — but if you `dotnet run --no-launch-profile`, set the env var explicitly.
2. The after sample's [appsettings.Development.json](../samples/after-agents-sdk/appsettings.Development.json) sets `TokenValidation:Enabled = false`. The user-secret entry above **overrides it back to true** so the JWT bearer middleware runs against real Azure Bot tokens.

---

## 3. Open a dev tunnel to localhost

The before sample binds `http://localhost:5000`; the after sample binds `http://localhost:5001` (both pinned in their `Properties/launchSettings.json`). Azure Bot Service needs an **HTTPS** endpoint, so we tunnel — one tunnel with two ports forwards both samples at once.

```pwsh
# Persistent named tunnel (anonymous; safe for a dev loop)
devtunnel create agents-mig-demo --allow-anonymous

# IMPORTANT: declare HTTP upstream — your bots are plain HTTP on localhost.
# If you forget --protocol http, the tunnel will probe HTTPS upstream and return 502.
devtunnel port create agents-mig-demo -p 5000 --protocol http   # before sample
devtunnel port create agents-mig-demo -p 5001 --protocol http   # after sample

# Start the host (long-running — leave this terminal open).
# Ports added AFTER the host starts won't take effect until you restart it.
devtunnel host agents-mig-demo
```

The host prints two URLs, one per port:

```
Hosting port: 5000
Connect via browser: https://24r7mnkg.use.devtunnels.ms:5000, https://24r7mnkg-5000.use.devtunnels.ms
Hosting port: 5001
Connect via browser: https://24r7mnkg.use.devtunnels.ms:5001, https://24r7mnkg-5001.use.devtunnels.ms
```

Capture both "without `:port`" URLs. Then point the Azure Bot at **whichever sample** you want Teams to talk to right now — you flip between them by re-running `az bot update` with a different endpoint:

```pwsh
$env:DEMO_TUNNEL_BEFORE = 'https://24r7mnkg-5000.use.devtunnels.ms'
$env:DEMO_TUNNEL_AFTER  = 'https://24r7mnkg-5001.use.devtunnels.ms'

# Point at the BEFORE sample (Bot Framework v4)
az bot update -g $env:DEMO_RG -n $env:DEMO_NAME --endpoint "$env:DEMO_TUNNEL_BEFORE/api/messages"

# … try it in Teams …

# Flip to the AFTER sample (Agents SDK) — same Teams chat, same Azure Bot, no restart
az bot update -g $env:DEMO_RG -n $env:DEMO_NAME --endpoint "$env:DEMO_TUNNEL_AFTER/api/messages"
```

That “flip” demonstrates the migration: same Teams app, same conversation, the SDK underneath swaps in seconds.

---

## 4. Build the Teams app package

The repo ships [teams-app/manifest.json](../teams-app/manifest.json), [teams-app/color.png](../teams-app/color.png), and [teams-app/outline.png](../teams-app/outline.png) (placeholder icons). The bot ID inside the manifest is currently the AppID this walkthrough used — replace it with `$env:DEMO_APPID`.

```pwsh
# Patch the bot id in the manifest (PowerShell)
$mf = Get-Content teams-app\manifest.json -Raw | ConvertFrom-Json
$mf.bots[0].botId = $env:DEMO_APPID
$mf.copilotAgents.customEngineAgents[0].id = $env:DEMO_APPID
$mf | ConvertTo-Json -Depth 12 | Set-Content teams-app\manifest.json -Encoding utf8

# Re-zip without a parent folder (Teams rejects nested packages)
$zip = 'teams-app\teams-app.zip'
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive `
    -Path teams-app\manifest.json, teams-app\color.png, teams-app\outline.png `
    -DestinationPath $zip
```

Replace `color.png` (192×192) and `outline.png` (32×32) with real branded icons before sharing the package — the placeholders are minimal solid-color glyphs.

---

## 5. Sideload in Teams

1. Open Teams in the **same tenant** the bot is registered in (`$env:DEMO_TENANTID`).
2. **Apps → Manage your apps → Upload an app → Upload a custom app**.
3. Pick `teams-app/teams-app.zip`.
4. Click **Add** and start a 1:1 chat.

If sideload is disabled, ask a tenant admin to enable **Allow custom apps** in the Teams admin center, or test in a developer-program tenant where it's on by default.

---

## 6. Run the samples, talk to them from Teams

You can run **both samples at once** — they bind different ports:

```pwsh
# Terminal A — before sample on http://localhost:5000
cd samples\before-bot-framework
dotnet run

# Terminal B — after sample on http://localhost:5001
cd samples\after-agents-sdk
dotnet run
```

Send `hello` in the Teams chat. You should see:

1. An adaptive welcome card on first message
2. `What's your name?`
3. After you reply, `Save 'Yourname' as your name?`
4. After `yes`, `Got it, Yourname. Send another message and I'll echo it back.`

To **switch SDKs without stopping anything**, just re-point the Azure Bot at the other tunnel port:

```pwsh
az bot update -g $env:DEMO_RG -n $env:DEMO_NAME --endpoint "$env:DEMO_TUNNEL_AFTER/api/messages"
```

Refresh the Teams chat. Same conversation, same Azure Bot, same Teams app, **different SDK**. That's the whole point of the side-by-side.

---

## 7. Optional — quick smoke test without opening Teams

[scripts/dl-smoketest.ps1](../scripts/dl-smoketest.ps1) drives a real conversation through the Azure Bot's DirectLine channel. Useful when you want to verify the deployed loop without launching Teams.

```pwsh
# DirectLine is auto-enabled on every Azure Bot. Grab the secret:
$env:DL_SECRET = az bot directline show `
    -g $env:DEMO_RG -n $env:DEMO_NAME --with-secrets true `
    --query "properties.properties.sites[0].key" -o tsv

# Send hello / name / yes and print bot replies
.\scripts\dl-smoketest.ps1
```

Expected output for either sample:

```
USER: hello
BOT : What's your name?
BOT : [attachment: application/vnd.microsoft.card.adaptive]
USER: <name>
BOT : Save '<name>' as your name?
USER: yes
BOT : Got it, <name>. Send another message and I'll echo it back.
```

Identical bytes from both samples = migration parity confirmed.

---

## 8. Optional — surface the after sample in Microsoft 365 Copilot

Manifest version `1.19` already includes a `copilotAgents.customEngineAgents` block:

```json
"copilotAgents": {
  "customEngineAgents": [
    { "type": "bot", "id": "<your bot id>" }
  ]
}
```

In tenants where Microsoft 365 Copilot is licensed and **custom-engine agents** are enabled, the same sideloaded package will also be reachable from the Copilot app. The before sample (BF v4) is **not** supported as a custom-engine agent — Microsoft 365 Copilot only accepts agents built on the Agents SDK or Foundry. That's part of the migration story: same Teams behaviour today, plus a path into Copilot when you're ready.

---

## 9. Clean up

```pwsh
# Stop the dev tunnel host (Ctrl+C in that terminal), then optionally:
devtunnel delete agents-mig-demo --force

# Tear down Azure
az group delete -n $env:DEMO_RG --yes --no-wait
az ad app delete --id $env:DEMO_APPID
```

Note: `dotnet user-secrets` are stored under `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`, **not** in the repo. Delete those folders if you want to wipe the credentials from the dev box.

---

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `az bot create` fails with `Multitenant bot creation is deprecated` | Your CLI version requires `SingleTenant` or `UserAssignedMSI` | Use `--app-type SingleTenant --tenant-id <tenant>` and an `AzureADMyOrg` Entra app |
| Dev tunnel returns `502 Bad Gateway` for any request | Tunnel was created with `--protocol https` but the bot listens on plain HTTP | Recreate the port: `devtunnel port delete agents-mig-demo -p 5000; devtunnel port create agents-mig-demo -p 5000 --protocol http` |
| Bot replies in Bot Framework Emulator but not from Teams | Azure Bot endpoint not updated, or dev tunnel host not running | `az bot show -g <rg> -n <bot> --query properties.endpoint`; restart `devtunnel host agents-mig-demo` |
| After sample boots but rejects every Teams call with 401 | `TokenValidation:Enabled=false` left in user-secrets, or `Audiences` doesn't match the bot's AppID | `dotnet user-secrets list --project samples\after-agents-sdk` and verify the values from step 2 |
| `az bot directline show` returns nothing | DirectLine site is missing on a fresh bot | `az bot directline create -g <rg> -n <bot>` |
| `dotnet user-secrets` set successfully but the app doesn't pick them up | Running with `ASPNETCORE_ENVIRONMENT != Development` | Set `$env:ASPNETCORE_ENVIRONMENT='Development'` before `dotnet run`, or use the launch profile |

For the broader `TokenValidation` / `Connections` story (8 supported `AuthType` values, gov clouds, federated identity), see [05-auth-and-azure-resources.md](05-auth-and-azure-resources.md).

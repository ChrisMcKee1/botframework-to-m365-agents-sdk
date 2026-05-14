# 05 — Authentication and Azure resources

Backing notes: [`../research/08-authentication-msal.md`](../research/08-authentication-msal.md). Citations: [`../research/sources.md`](../research/sources.md).

## What stays — and it's most of it

Your migration can reuse:

| Resource | Notes |
|---|---|
| Azure Bot registration | Same resource, same App ID, same channels. |
| Microsoft Entra app registration | Same App ID / client secret (or certificate / managed identity). |
| Hosting (App Service / Functions / AKS) | No change required. |
| Channel registrations (Teams, Web Chat, …) | Bound to the bot, untouched. |
| App Insights resource | Keep — but rewire telemetry to standard observability (the BF telemetry helpers are gone). |

Migrate the **SDK** and the **app settings**. The Azure topology is unchanged.

## What changes — two new app settings sections

In Bot Framework SDK, the adapter validated inbound JWTs and acquired outbound tokens internally, driven by `MicrosoftAppType` / `MicrosoftAppId` / `MicrosoftAppPassword` / `MicrosoftAppTenantId`.

In Agents SDK, these two jobs are separated:

| Job | Block | Owned by |
|---|---|---|
| **Inbound JWT validation** | `TokenValidation` | ASP.NET (`AddAgentAspNetAuthentication`) / aiohttp middleware / Express `authorizeJWT` |
| **Outbound token acquisition** (Bot Connector, downstream APIs) | `Connections` + `ConnectionsMap` | MSAL via `Microsoft.Agents.Authentication.Msal` |

### `TokenValidation` (inbound)

```json
"TokenValidation": {
  "Enabled": true,
  "Audiences": [ "{{MicrosoftAppId}}" ],
  "TenantId": "{{MicrosoftTenantId}}"
}
```

All available settings are documented as comments in [`AspNetExtensions.cs`](https://github.com/microsoft/Agents/blob/main/samples/dotnet/quickstart/AspNetExtensions.cs).

### `Connections` (outbound)

A named connection profile with `AuthType` + settings:

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
}
```

### `ConnectionsMap` (routing)

Routes outbound service URLs to a connection profile. `"*"` matches all.

```json
"ConnectionsMap": [
  { "ServiceUrl": "*", "Connection": "ServiceConnection" }
]
```

## What to remove (after validation)

The legacy app settings are **harmless during migration** but unused by the new SDK. Remove after the new blocks are verified:

- `MicrosoftAppType`
- `MicrosoftAppId`
- `MicrosoftAppPassword`
- `MicrosoftAppTenantId`

## Choosing an `AuthType`

The MSAL provider supports eight identity strategies. Pick by hosting model:

| If hosting on… | Recommend |
|---|---|
| App Service / Functions, single-tenant app | `ClientSecret` (single-tenant authority) |
| App Service / Functions, multi-tenant app | `ClientSecret` (`botframework.com` authority) |
| App Service / Functions with system-assigned MI | `SystemManagedIdentity` |
| App Service / Functions with user-assigned MI | `UserManagedIdentity` |
| AKS with workload identity | `WorkloadIdentity` |
| Federated credentials (WIF) | `FederatedCredentials` |
| Cert in cert store (rotation) | `CertificateSubjectName` (use `SendX5C: true`) |
| Cert in cert store (thumbprint-pinned) | `Certificate` |

### Single-tenant `ClientSecret` example (lowest-friction)

```json
"Connections": {
  "ServiceConnection": {
    "Settings": {
      "AuthType": "ClientSecret",
      "AuthorityEndpoint": "https://login.microsoftonline.com/{{TENANT_ID}}",
      "ClientId": "{{APP_ID}}",
      "ClientSecret": "{{APP_SECRET}}",
      "Scopes": [ "https://api.botframework.com/.default" ]
    }
  }
}
```

### Multi-tenant `ClientSecret` example

```json
"AuthorityEndpoint": "https://login.microsoftonline.com/botframework.com"
```

### System-assigned managed identity

```json
{
  "AuthType": "SystemManagedIdentity",
  "Scopes": [ "https://api.botframework.com/.default" ]
}
```

### User-assigned managed identity

```json
{
  "AuthType": "UserManagedIdentity",
  "ClientId": "{{UAMI_CLIENT_ID}}",
  "Scopes": [ "https://api.botframework.com/.default" ]
}
```

### Workload identity (AKS)

```json
{
  "AuthType": "WorkloadIdentity",
  "ClientId": "{{APP_ID}}",
  "AuthorityEndpoint": "https://login.microsoftonline.com/{{TENANT_ID}}",
  "FederatedTokenFile": "{{TOKEN_FILE_PATH}}",
  "Scopes": [ "https://api.botframework.com/.default" ]
}
```

### Certificate by subject name (rotation-friendly)

```json
{
  "AuthType": "CertificateSubjectName",
  "ClientId": "{{APP_ID}}",
  "CertSubjectName": "{{CERT_SUBJECT_NAME}}",
  "SendX5C": true,
  "AuthorityEndpoint": "https://login.microsoftonline.com/{{TENANT_ID}}",
  "Scopes": [ "https://api.botframework.com/.default" ]
}
```

Full list (eight types) and all knobs: [`../research/08-authentication-msal.md`](../research/08-authentication-msal.md).

## MSAL-wide tuning (optional)

```json
"MSALConfiguration": {
  "MSALRequestTimeout": "00:00:40",
  "MSALRetryCount": "1",
  "MSALEnabledLogPII": "false"
}
```

`MSALEnabledLogPII` should stay `false` in production. Set `true` only when actively diagnosing a token issue in a non-production environment.

## MSAL logging when diagnosing token issues

Add to `appsettings.Development.json` only:

```json
"Logging": {
  "LogLevel": {
    "Default": "Warning",
    "Microsoft.Agents": "Information",
    "Microsoft.Agents.Authentication.Msal": "Trace"
  }
}
```

## Recommendation for a first migration pass

1. Keep the existing single-tenant App ID + secret.
2. Use `AuthType: "ClientSecret"` with the single-tenant authority.
3. Validate the new bot works end-to-end against the existing Azure Bot resource.
4. **Then** consider moving to managed identity in a second pass.

This minimizes the surface area of the SDK migration so failures are easier to triage.

## Next

→ [`06-ai-orchestration-options.md`](./06-ai-orchestration-options.md)

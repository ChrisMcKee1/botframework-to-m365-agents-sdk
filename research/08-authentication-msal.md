# Authentication — MSAL, TokenValidation, Connections

Sources:
- https://learn.microsoft.com/microsoft-365/agents-sdk/microsoft-authentication-library-configuration-options
- https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-dotnet (§ appsettings)
- https://github.com/microsoft/Agents/blob/main/samples/dotnet/quickstart/AspNetExtensions.cs

Verified 2026-05-14. The MSAL doc is .NET-centric; Node and Python pages cover their respective auth wiring but the underlying concepts (`Connections`, identity types) are the same.

## Two distinct concerns, two config blocks

| Block | Purpose |
|---|---|
| **`TokenValidation`** | Validates the JWT on **inbound** requests from Azure Bot Service / Entra (HTTP auth — handed to ASP.NET / aiohttp / Express). |
| **`Connections`** | Acquires tokens for **outbound** calls — to Bot Connector, Teams, downstream APIs, other agents. Powered by MSAL via `Microsoft.Agents.Authentication.Msal`. |

In Bot Framework SDK both jobs were done by the adapter. In Agents SDK they are separated:

- Inbound JWT validation is now framework-owned (ASP.NET via `AddAgentAspNetAuthentication`, Express via `authorizeJWT`, aiohttp via SDK middleware).
- Outbound token acquisition is owned by an `IConnections` instance configured from `appsettings`.

## Inbound — `TokenValidation` (.NET appsettings.json)

```json
"TokenValidation": {
  "Enabled": true,
  "Audiences": [
    "{{MicrosoftAppId-value}}"
  ],
  "TenantId": "{{MicrosoftTenantId-value}}"
}
```

All available settings are documented as comments in [`AspNetExtensions.cs`](https://github.com/microsoft/Agents/blob/main/samples/dotnet/quickstart/AspNetExtensions.cs). Copy that file into the project or rely on `AddAgentAspNetAuthentication(builder.Configuration)`.

## Outbound — `Connections` and `ConnectionsMap`

Two parts:

1. **`Connections`** — named connection profiles, each with an `AuthType` + settings.
2. **`ConnectionsMap`** — routes outbound service URLs to a connection profile.

```json
"ConnectionsMap": [
  { "ServiceUrl": "*", "Connection": "ServiceConnection" }
]
```

`"*"` matches all outbound service URLs. You can have multiple maps to direct different downstream endpoints through different profiles.

## All seven supported `AuthType` values

The MSAL provider supports the following identity strategies:

### 1. `ClientSecret` (single-tenant)

```json
"Connections": {
  "ServiceConnection": {
    "Settings": {
      "AuthType": "ClientSecret",
      "ClientId": "{{BOT_ID}}",
      "ClientSecret": "{{BOT_SECRET}}",
      "AuthorityEndpoint": "https://login.microsoftonline.com/{{BOT_TENANT_ID}}",
      "Scopes": [ "https://api.botframework.com/.default" ]
    }
  }
}
```

### 2. `ClientSecret` (multi-tenant)

Same shape, but use the `botframework.com` authority:

```json
"AuthorityEndpoint": "https://login.microsoftonline.com/botframework.com"
```

### 3. `UserManagedIdentity`

Host must run with a user-assigned managed identity attached.

```json
{
  "AuthType": "UserManagedIdentity",
  "ClientId": "{{BOT_ID}}",
  "Scopes": [ "https://api.botframework.com/.default" ]
}
```

### 4. `SystemManagedIdentity`

Host must run with a system-assigned managed identity. `ClientId` is ignored.

```json
{
  "AuthType": "SystemManagedIdentity",
  "Scopes": [ "https://api.botframework.com/.default" ]
}
```

### 5. `FederatedCredentials`

For Workload Identity Federation against a federated client.

```json
{
  "AuthType": "FederatedCredentials",
  "ClientId": "{{BOT_ID}}",
  "AuthorityEndpoint": "https://login.microsoftonline.com/{{BOT_TENANT_ID}}",
  "FederatedClientId": "{{BOT_FEDERATED_ID}}",
  "Scopes": [ "https://api.botframework.com/.default" ]
}
```

### 6. `WorkloadIdentity`

For AKS workload identity (federated token file).

```json
{
  "AuthType": "WorkloadIdentity",
  "ClientId": "{{BOT_ID}}",
  "AuthorityEndpoint": "https://login.microsoftonline.com/{{BOT_TENANT_ID}}",
  "FederatedTokenFile": "{{BOT_FEDERATED_TOKENFILE}}",
  "Scopes": [ "https://api.botframework.com/.default" ]
}
```

Optional client-assertion sub-block (`AssertionRequestOptions`) supports `ClientId`, `TokenEndpoint`, `Claims`, `ClientCapabilities`.

### 7. `CertificateSubjectName` (SN+I rotation)

```json
{
  "AuthType": "CertificateSubjectName",
  "ClientId": "{{BOT_ID}}",
  "CertSubjectName": "{{BOT_CERT_SUBJECTNAME}}",
  "SendX5C": true,
  "AuthorityEndpoint": "https://login.microsoftonline.com/{{BOT_TENANT_ID}}",
  "Scopes": [ "https://api.botframework.com/.default" ]
}
```

Cert store defaults to `"My"`. `ValidCertificateOnly` defaults to `true`. `SendX5C: true` enables auto-rotation with appropriate config.

### 8. `Certificate` (by thumbprint)

```json
{
  "AuthType": "Certificate",
  "ClientId": "{{BOT_ID}}",
  "CertThumbprint": "{{BOT_CERT_THUMBPRINT}}",
  "AuthorityEndpoint": "https://login.microsoftonline.com/botframework.com",
  "Scopes": [ "https://api.botframework.com/.default" ]
}
```

## MSAL-wide knobs (`MSALConfiguration` section)

Optional, separate top-level config section that tunes MSAL for all clients:

```json
"MSALConfiguration": {
  "MSALEnabledLogPII": "true",
  "MSALRequestTimeout": "00:00:40",
  "MSALRetryCount": "1"
}
```

| Setting | Default | Purpose |
|---|---|---|
| `MSALRequestTimeout` | 30s | Wait for response from Entra ID. |
| `MSALRetryCount` | 3 | Retry attempts per token request. |
| `MSALEnabledLogPII` | false | Allow PII in MSAL logs (diagnostic only — turn off in prod). |

## Default MSAL configuration provider

When you use `builder.AddAgent<MyAgent>()`, the SDK auto-registers a default `IConnections` instance backed by `ConfigurationConnections` (reads from `appsettings.json` `Connections` section).

If you bypass `AddAgent`:

```csharp
builder.Services.AddSingleton<IConnections, ConfigurationConnections>();
builder.Services.AddDefaultMsalAuth(builder.Configuration);
```

## MSAL logging

Add a logging filter for the MSAL module:

```json
"Logging": {
  "LogLevel": {
    "Default": "Warning",
    "Microsoft.Agents": "Warning",
    "Microsoft.Hosting.Lifetime": "Information",
    "Microsoft.Agents.Authentication.Msal": "Trace"
  }
}
```

Set to `Trace` only while diagnosing token issues — and only combine with `MSALEnabledLogPII: true` in non-prod environments.

## Picking an `AuthType`

The right `AuthType` depends on hosting:

| If they host on… | Recommend |
|---|---|
| App Service / Functions with system-assigned MI | `SystemManagedIdentity` |
| App Service / Functions with user-assigned MI | `UserManagedIdentity` |
| AKS | `WorkloadIdentity` |
| Anywhere with a cert in the cert store | `Certificate` or `CertificateSubjectName` |
| Migration in flight, want minimum change | `ClientSecret` (use existing AppId/Password) |

This is question 9 on the discovery checklist (auth model). The current production bot is almost certainly on `MicrosoftAppId` + `MicrosoftAppPassword` — start there, migrate to MI in a second pass.

# Node.js / JavaScript migration deep-dive

Source: https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-nodejs. Verified 2026-05-14.

## 1. Runtime

- **Node.js 20+** per the migration guide. (Overview page allows 18+; the migration guide is stricter — use 20.)

## 2. Package substitutions

| Concern | Bot Framework | Microsoft 365 Agents SDK |
|---|---|---|
| Core hosting | `botbuilder` | `@microsoft/agents-hosting` |
| Activity schema | `botframework-schema` | `@microsoft/agents-activity` |
| Dialogs | `botbuilder-dialogs` | `@microsoft/agents-hosting-dialogs` |
| Azure Cosmos DB | `botbuilder-azure` | `@microsoft/agents-hosting-storage-cosmos` |
| Azure Blob Storage | `botbuilder-azure-blobs` | `@microsoft/agents-hosting-storage-blob` |
| Express server utilities | Manual | `@microsoft/agents-hosting-express` |
| Teams | (parts of `botbuilder`) | `@microsoft/agents-hosting-extensions-teams` |

## 3. `require` / `import` rewrites

| Find | Replace |
|---|---|
| `require('botframework-schema')` | `require('@microsoft/agents-activity')` |
| `require('botbuilder')` | `require('@microsoft/agents-hosting')` |
| `require('botbuilder-dialogs')` | `require('@microsoft/agents-hosting-dialogs')` |

## 4. Activity class — now Zod-validated

- `@microsoft/agents-activity` ships the new `Activity` class. Validation backed by [zod](https://zod.dev/).
- Parse from raw JSON with `Activity.fromJson(...)`, from a JS object with `Activity.fromObject(...)`.
- Methods that used to be static on `TurnContext` are now instance methods on `Activity`:

| Old | New |
|---|---|
| `TurnContext.applyConversationReference` | `activity.applyConversationReference` |
| `TurnContext.getConversationReference` | `activity.getConversationReference` |
| `TurnContext.getReplyConversationReference` | `activity.getReplyConversationReference` |
| `TurnContext.removeRecipientMention` | `activity.removeRecipientMention` |
| `TurnContext.getMentions` | `activity.getMentions` |
| `TurnContext.removeMentionText` | `activity.removeMentionText` |

## 5. Environment variables

Different from Python's double-underscore style — Node uses flat lowercase keys:

```env
clientId=your-app-id
clientSecret=your-app-secret
tenantId=your-tenant-id
PORT=3978
DEBUG=true
```

Migration:

| Old | New |
|---|---|
| `MicrosoftAppId` | `clientId` |
| `MicrosoftAppPassword` | `clientSecret` |
| `MicrosoftAppTenantId` | `tenantId` |

## 6. Authentication — JWT moves to your HTTP server

Bot Framework SDK validated JWTs inside the adapter. The Agents SDK gives you `authorizeJWT(AuthConfiguration)` middleware so Express handles it.

```javascript
import { authorizeJWT, loadAuthConfigFromEnv } from '@microsoft/agents-hosting'

const authConfig = loadAuthConfigFromEnv()
server.use(authorizeJWT(authConfig))
```

`ConfigurationBotFrameworkAuthentication` is replaced by the `AuthConfiguration` interface, loaded with `loadAuthConfigFromEnv()`.

Local-dev escape:

```javascript
if (process.env.NODE_ENV === 'development') {
  // JWT validation disabled for local testing
} else {
  server.use(authorizeJWT(authConfig))
}
```

## 7. Two server-setup options

### Option A — `startServer()` (minimal)

```javascript
const { EchoBot } = require('./bot')
const { startServer } = require('@microsoft/agents-hosting-express')
startServer(new EchoBot())
```

### Option B — manual Express (for migration / custom middleware)

```javascript
const { EchoBot } = require('./bot')
const {
  CloudAdapter,
  loadAuthConfigFromEnv,
  authorizeJWT,
} = require('@microsoft/agents-hosting')

const authConfig = loadAuthConfigFromEnv()
const adapter = new CloudAdapter(authConfig)
const myBot = new EchoBot()

const server = express()
server.use(express.json())
server.use(authorizeJWT(authConfig))

server.post('/api/messages', async (req, res) =>
  await adapter.process(req, res, (context) => myBot.run(context)))

const port = process.env.PORT || 3978
server.listen(port, () => console.log(`Server listening on port ${port}`))
```

## 8. `ActivityHandler` is supported but deprecated

The SDK ships a compatible `ActivityHandler` (`@microsoft/agents-hosting`) for low-friction lift-and-shift. **The recommended new shape is `AgentApplication`.**

Handler signature change: handler functions use the `AgentHandler` type (same signature as `BotHandler`). Methods now return `ActivityHandler` (the class) instead of `this`.

Added handler methods:

- `onMessageDelete`
- `onMessageUpdate`
- `onSignInInvoke`

Removed handler methods (don't try to override):

- `onCommand`
- `onCommandResult`
- `onEvent` — generic event handling is gone; specific event types like `onTokenResponseEvent` still work
- `onTokenResponseEvent` — *(per the table the article lists this both as removed and as the exception to "generic event handling is gone"; treat as: don't bind generic `onEvent`, do bind specific event types)*

## 9. `ActivityHandler` → `AgentApplication`

| Concern | `ActivityHandler` | `AgentApplication` |
|---|---|---|
| State management | Manual | Built-in via `storage` option |
| Event handling | Generic (`onMembersAdded`) | Specific events (`membersAdded` discriminator) |
| `next()` | Required in handlers | Not required |
| Storage | Manual config | Built-in, with automatic state persistence |

Minimal `AgentApplication`:

```javascript
import { AgentApplication, MemoryStorage } from '@microsoft/agents-hosting'

const agent = new AgentApplication({ storage: new MemoryStorage() })

agent.onMessage('/count', async (context, state) => {
  const count = state.conversation.count ?? 0
  state.conversation.count = count + 1
  await context.sendActivity(`Count: ${state.conversation.count}`)
})
```

For larger bots, subclass it (the MS Learn page shows a multi-route `MyAgent extends AgentApplication` example with `/help`, `/status`, `/reset` commands — arrow-function handlers preserve `this`).

## 10. Migration checklist

- [ ] Identify unsupported features (LUIS / QnA / Composer / LG / generic `onEvent`). Decide migrate vs. rebuild.
- [ ] Upgrade Node.js to ≥20.
- [ ] Replace `botbuilder*` packages with `@microsoft/agents-*` equivalents.
- [ ] Find/replace imports and `require` calls.
- [ ] Rename env vars (`MicrosoftAppId` → `clientId`, etc.).
- [ ] Choose server setup — `startServer()` or manual Express + `authorizeJWT`.
- [ ] Update activity-handling methods, particularly anything that used static `TurnContext` helpers.
- [ ] Decide: stay on `ActivityHandler` (compat) or refactor to `AgentApplication` (recommended).
- [ ] Validate against Emulator / Agents Playground.
- [ ] Update environment variables in Azure; deploy; monitor.

# Python migration deep-dive

Source: https://learn.microsoft.com/microsoft-365/agents-sdk/bf-migration-python. Verified 2026-05-14.

## 1. Runtime

- Python **3.10 or higher** (3.11+ recommended). Migration guide notes support up to 3.14.
- The overview page lists 3.9–3.11; **the migration guide is stricter — go with 3.10 minimum**.

## 2. Package substitutions (`requirements.txt`)

| Bot Framework | Microsoft 365 Agents SDK |
|---|---|
| `botbuilder-core` | `microsoft-agents-hosting-core` |
| `botbuilder-schema` | `microsoft-agents-activity` |
| `botbuilder-azure` (Cosmos + Blob) | `microsoft-agents-storage-blob` and `microsoft-agents-storage-cosmos` |
| `botbuilder-integration-aiohttp` | `microsoft-agents-hosting-aiohttp` |
| Teams: `botbuilder-core` Teams parts | `microsoft-agents-hosting-teams` |
| Auth: SDK-internal | `microsoft-agents-authentication-msal` |

Also recommended: add `black` (formatting) and `flake8` (linting) — the SDK uses them as quality gates.

## 3. The most common migration error: dots → underscores

> The Agents SDK uses underscores in import paths (`microsoft_agents`) instead of dots (`microsoft.agents`).

This trips everyone. Project-wide find/replace:

| Find | Replace |
|---|---|
| `from botbuilder.core import …` | `from microsoft_agents.hosting.core import …` |
| `from botbuilder.schema import …` | `from microsoft_agents.activity import …` |
| `from botbuilder.integration.aiohttp import …` | `from microsoft_agents.hosting.aiohttp import …` |
| `from botbuilder.core.teams import …` | `from microsoft_agents.hosting.teams import …` |

If you see `ModuleNotFoundError: No module named 'microsoft.agents'`, you used dots. Switch to underscores.

## 4. Type / class renames

| Old | New |
|---|---|
| `BotState` | `AgentState` |
| `BotFrameworkAdapter` | `CloudAdapter` |
| `BotFrameworkHttpClient` | `AgentHttpClient` |
| `OAuthPromptSettings.connection_name` | `OAuthPromptSettings.azure_bot_oauth_connection_name` |

## 5. Activity class — now Pydantic-validated

- The new `Activity` class uses [Pydantic](https://docs.pydantic.dev/) for validation.
- Parse with `Activity.model_validate()` from JSON / dict.
- Several methods moved off `TurnContext` (where they were static helpers) onto the `Activity` instance:

| Old (TurnContext static) | New (Activity instance) |
|---|---|
| `TurnContext.apply_conversation_reference()` | `activity.apply_conversation_reference()` |
| `TurnContext.get_conversation_reference()` | `activity.get_conversation_reference()` |
| `TurnContext.get_reply_conversation_reference()` | `activity.get_reply_conversation_reference()` |
| `TurnContext.remove_recipient_mention()` | `activity.remove_recipient_mention()` |
| `TurnContext.get_mentions()` | `activity.get_mentions()` |
| `TurnContext.remove_mention_text()` | `activity.remove_mention_text()` |

Turn state access:

| Old | New |
|---|---|
| `turn_context.turn_state.get(ConnectorClient)` | `turn_context.services.get(ConnectorClient)` |
| `turn_context.turn_state.get(UserTokenClient)` | `turn_context.services.get(UserTokenClient)` |
| `turn_context.turn_state` | `turn_context.services` |

## 6. Configuration — env vars with double underscores

The Agents SDK uses hierarchical env vars. The separator is **double underscore (`__`)**.

```env
# Required
CONNECTIONS__SERVICE_CONNECTION__SETTINGS__CLIENTID=your-app-id
CONNECTIONS__SERVICE_CONNECTION__SETTINGS__CLIENTSECRET=your-app-secret
CONNECTIONS__SERVICE_CONNECTION__SETTINGS__TENANTID=your-tenant-id

# Optional - local debugging
PORT=3978
```

Migration of legacy env vars:

| Old | New |
|---|---|
| `APP_ID` / `MICROSOFT_APP_ID` | `CONNECTIONS__SERVICE_CONNECTION__SETTINGS__CLIENTID` |
| `APP_PASSWORD` / `MICROSOFT_APP_PASSWORD` | `CONNECTIONS__SERVICE_CONNECTION__SETTINGS__CLIENTSECRET` |
| `APP_TENANT_ID` / `MICROSOFT_APP_TENANT_ID` | `CONNECTIONS__SERVICE_CONNECTION__SETTINGS__TENANTID` |

Local-only escape hatch (Emulator without auth):

```env
CONNECTIONS__SERVICE_CONNECTION__SETTINGS__ANONYMOUS_ALLOWED=True
```

## 7. Startup — the recommended pattern is `AgentApplication` + decorators

Bot Framework Python used aiohttp + manual `BotFrameworkAdapter` wiring. The Agents SDK encourages a declarative decorator pattern.

```python
import re
from os import environ
from dotenv import load_dotenv

from microsoft_agents.hosting.aiohttp import CloudAdapter
from microsoft_agents.hosting.core import (
    Authorization,
    AgentApplication,
    TurnState,
    TurnContext,
    MemoryStorage,
)
from microsoft_agents.authentication.msal import MsalConnectionManager
from microsoft_agents.activity import load_configuration_from_env

load_dotenv()
agents_sdk_config = load_configuration_from_env(environ)

STORAGE = MemoryStorage()
CONNECTION_MANAGER = MsalConnectionManager(**agents_sdk_config)
ADAPTER = CloudAdapter(connection_manager=CONNECTION_MANAGER)
AUTHORIZATION = Authorization(STORAGE, CONNECTION_MANAGER, **agents_sdk_config)

AGENT_APP = AgentApplication[TurnState](
    storage=STORAGE,
    adapter=ADAPTER,
    authorization=AUTHORIZATION,
    **agents_sdk_config,
)

@AGENT_APP.conversation_update("membersAdded")
async def on_members_added(context: TurnContext, _state: TurnState):
    await context.send_activity("Welcome!")
    return True

@AGENT_APP.activity("message")
async def on_message(context: TurnContext, _state: TurnState):
    await context.send_activity(f"You said: {context.activity.text}")
```

Notes:

- `load_configuration_from_env()` parses the hierarchical env vars automatically.
- `ActivityHandler` is still available (compatible API) — useful for a lower-friction first pass before refactoring to decorators.

## 8. State management

`ConversationState`, `UserState`, `PrivateConversationState` keep the same API shape. You **must** call `save_changes` after writes — the decorator pattern doesn't change this:

```python
STORAGE = MemoryStorage()
conversation_state = ConversationState(STORAGE)
user_state = UserState(STORAGE)
count_property = conversation_state.create_property("conversation_data")

@AGENT_APP.activity("message")
async def on_message(context: TurnContext, _state: TurnState):
    data = await count_property.get(context, {})
    data["count"] = data.get("count", 0) + 1
    await count_property.set(context, data)
    await context.send_activity(f"Message count: {data['count']}")
    await conversation_state.save_changes(context)
    await user_state.save_changes(context)
```

Storage providers:

```python
from microsoft_agents.hosting.core import MemoryStorage
from microsoft_agents.storage.blob import BlobStorage
from microsoft_agents.storage.cosmos import CosmosDbPartitionedStorage
```

## 9. ActivityHandler — added / missing methods

Added in Agents SDK:

- `on_message_delete()` — message deletion activities
- `on_message_update()` — message update activities
- `on_sign_in_invoke()` — sign-in invoke activities

Missing from Agents SDK (don't try to override these — they're gone):

- `on_command_activity()`
- `on_command_result_activity()`

## 10. Logging

Standard Python `logging`. No SDK-specific helpers.

```python
import logging
ms_agents_logger = logging.getLogger("microsoft_agents")
handler = logging.StreamHandler()
handler.setFormatter(logging.Formatter("%(asctime)s - %(name)s - %(levelname)s - %(message)s"))
ms_agents_logger.addHandler(handler)
ms_agents_logger.setLevel(logging.INFO)
```

## 11. Troubleshooting cheatsheet

| Symptom | Cause / fix |
|---|---|
| `ModuleNotFoundError: microsoft.agents` | Dots vs underscores. Use `microsoft_agents.*`. |
| Config seems ignored | Env var naming. Double-underscore (`__`) is required for hierarchy. |
| 401 in Emulator | Set `CONNECTIONS__SERVICE_CONNECTION__SETTINGS__ANONYMOUS_ALLOWED=True` for local dev, or supply real credentials in the Emulator. |
| Pydantic validation errors on inbound activity | Use `Activity.model_validate(payload)` and check that the channel actually populated the field you're reading. |

## 12. Migration checklist

- [ ] Identify deprecated features (LUIS / QnA / Composer / LG). Decide migrate vs. rebuild.
- [ ] Upgrade Python to ≥3.10 (3.11+ recommended).
- [ ] Replace `botbuilder-*` packages with `microsoft-agents-*` equivalents.
- [ ] Find/replace imports — **dots → underscores**.
- [ ] Update renamed classes, move `TurnContext` static methods onto `Activity` instances.
- [ ] Convert `.env` to double-underscore hierarchical keys.
- [ ] Rewire startup to `CloudAdapter` + `MsalConnectionManager` + `AgentApplication`.
- [ ] Configure standard `logging` for `microsoft_agents` logger.
- [ ] Build and test against Emulator.
- [ ] Update Azure app settings; deploy; monitor.

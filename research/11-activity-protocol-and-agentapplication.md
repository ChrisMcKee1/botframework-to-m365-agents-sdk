# Activity Protocol + AgentApplication — programming model primer

Sources:
- https://learn.microsoft.com/microsoft-365/agents-sdk/activity-protocol
- https://learn.microsoft.com/microsoft-365/agents-sdk/agent-application

Verified 2026-05-14. C# snippets shown here mirror the MS Learn examples; JS/Python use the same shapes.

This is the conceptual background a developer needs before reading any language-specific migration doc. The whole SDK rotates around two things: `Activity` and `AgentApplication`.

## The `Activity` — every interaction is one

An `Activity` is a structured JSON object representing any interaction between user and agent. Not just text — also events, typing indicators, file uploads, card actions, custom events.

### Key properties

| Property | Purpose |
|---|---|
| `Id` | Channel-assigned identifier |
| `Type` | What the activity means — message, event, invoke, conversationUpdate, typing |
| `ChannelId` | Where it came from (e.g. `msteams`) |
| `From` | Sender (user or agent) |
| `Recipient` | Intended recipient |
| `Text` | Message text content |
| `Attachments` | Rich content (cards, images, files) |

Full spec: https://github.com/microsoft/Agents/blob/main/specs/activity/protocol-activity.md

### Activity types you handle

| Type | Notes |
|---|---|
| **Message** | The common case — text + attachments + suggested actions. |
| **ConversationUpdate** | Members join/leave. Not all channels emit it (Teams does). |
| **Event** | Custom structured event from the client. Inspect `activity.Name` and `activity.Value`. |
| **Invoke** | Client invokes a specific command (e.g., Teams `task/fetch`, `task/submit`). Not all channels support. |
| **Typing** | "Someone is typing". **Not supported in M365 Copilot.** Works in Teams. |

## The `TurnContext` — your handle on the current turn

Every incoming activity creates a fresh `TurnContext`. It exists for one turn and is disposed afterward.

A **turn** = one round trip:

1. Channel delivers an activity to your endpoint.
2. SDK creates a `TurnContext` and routes to your handler.
3. Your handler reads `Activity`, manipulates state, sends one or more responses.
4. SDK saves state, disposes `TurnContext`. Done.

Key data accessible via `TurnContext`:

- `Activity` — the inbound activity
- `Adapter` — the channel adapter
- `TurnState` (.NET 8 SDK) / `state` (JS / Python) — partitioned state for this turn
- `Services` — replaces `TurnState` for service lookups (e.g. `.Services.Get<IConnectorClient>()`)

### Common send patterns

```csharp
agent.OnActivity(ActivityTypes.Message, async (turnContext, turnState, ct) =>
{
    await turnContext.SendActivityAsync("hello!", cancellationToken: ct);                  // raw string
    await turnContext.SendActivityAsync(MessageFactory.Text("Hello"), ct);                 // factory
    await turnContext.SendActivitiesAsync(activities, ct);                                 // bulk
});
```

## `AgentApplication` — the center of the agent

`AgentApplication` is **the** building block. It's the entry point for every activity, the router, and the owner of turn state.

### Lifecycle

```
Channel → Hosting layer → AgentApplication → Your handlers
```

1. Hosting layer authenticates the HTTP request.
2. `AgentApplication` processes the activity through its pipeline.
3. Routes are evaluated; the matching handler runs.
4. State is loaded before handlers run; saved automatically afterward.

### Routes

A route = `(selector, handler)` pair. Selectors match on:

- Specific text (exact match, case-insensitive)
- A regex
- Any activity of a given type
- Conversation lifecycle events
- Adaptive card actions
- Custom predicates

### Evaluation order — two-level sort

Routes are sorted **once at registration**, not at runtime. Two levels:

1. **Route type group** (highest priority first):
   1. Agentic invoke routes
   2. Invoke routes (adaptive card actions, OAuth callbacks)
   3. Agentic routes
   4. All other routes
2. **Rank within a group** — lower numeric values first:
   - `RouteRank.First` = 0
   - `RouteRank.Unspecified` = 32767 (default)
   - `RouteRank.Last` = 65535

Only the first matching route runs by default. The idiomatic "catch-all" looks like this:

```csharp
OnMessage("status", HandleStatusAsync);
OnMessage("help", HandleHelpAsync);
OnActivity(ActivityTypes.Message, HandleUnknownMessageAsync, rank: RouteRank.Last);
```

### Turn lifecycle hooks (preferred over middleware for new code)

```csharp
OnBeforeTurn(async (context, state, ct) =>
{
    logger.LogInformation("Turn started: {Type}", context.Activity.Type);
    return true; // return false to abort the turn
});

OnAfterTurn(async (context, state, ct) =>
{
    logger.LogInformation("Turn completed");
    return true; // return false to skip state saving
});

OnTurnError(async (context, state, exception, ct) =>
{
    logger.LogError(exception, "Turn error");
    await context.SendActivityAsync("Something went wrong. Please try again.", cancellationToken: ct);
});
```

Old `IMiddleware` implementations are still supported via DI for migration continuity.

### Turn state — three scopes

| Scope | Lifetime | Use for |
|---|---|---|
| **Conversation** | Persisted per conversation | Counters, conversation context, dialog state |
| **User** | Persisted per user across all conversations | Preferences, display name |
| **Temp** | Current turn only | Parsed input, ephemeral derived data |

State is loaded before handlers run and saved automatically afterward.

```csharp
OnActivity(ActivityTypes.Message, async (context, state, ct) =>
{
    var count = state.Conversation.GetValue<int>("messageCount", () => 0);
    state.Conversation.SetValue("messageCount", count + 1);

    var name = state.User.GetValue<string>("displayName");

    state.Temp.SetValue("parsedInput", context.Activity.Text?.Trim());

    await context.SendActivityAsync($"Message #{count + 1}: {context.Activity.Text}", cancellationToken: ct);
});
```

For production, use `Microsoft.Agents.Storage.Blobs` or `Microsoft.Agents.Storage.CosmosDb` instead of `MemoryStorage`. See the migration docs for the configuration.

## Minimal `Program.cs` (modern `AgentApplication` pattern)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddAgent<MyAgent>();
builder.Services.AddAgentAspNetAuthentication(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapAgentApplicationEndpoints(requireAuth: !app.Environment.IsDevelopment());

app.Run();
```

`MapAgentApplicationEndpoints(requireAuth: …)` is the modern shorthand for `MapPost("/api/messages", …).RequireAuthorization()`. Either works; the migration guide shows the latter, the AgentApplication page shows the former.

## Minimal `AgentApplication` subclass

```csharp
public class MyAgent : AgentApplication
{
    public MyAgent(AgentApplicationOptions options) : base(options)
    {
        OnConversationUpdate(ConversationUpdateEvents.MembersAdded, WelcomeAsync);
        OnActivity(ActivityTypes.Message, OnMessageAsync, rank: RouteRank.Last);
    }

    private async Task WelcomeAsync(ITurnContext context, ITurnState state, CancellationToken ct)
    {
        foreach (var member in context.Activity.MembersAdded)
        {
            if (member.Id != context.Activity.Recipient.Id)
            {
                await context.SendActivityAsync("Hello! How can I help you?", cancellationToken: ct);
            }
        }
    }

    private async Task OnMessageAsync(ITurnContext context, ITurnState state, CancellationToken ct)
    {
        await context.SendActivityAsync($"You said: {context.Activity.Text}", cancellationToken: ct);
    }
}
```

That's the entire "modern shape" — `Program.cs` boots the host, `MyAgent : AgentApplication` registers routes, state is automatic. Compare with `samples/before-bot-framework/` (when scaffolded) for the BF v4 equivalent.

using MigrationSample.After.Agents;
using MigrationSample.After.Dialogs;
using Microsoft.Agents.Authentication.Msal;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

// Agents SDK — register the agent and the hosting plumbing (CloudAdapter etc.).
builder.AddAgent<UserProfileAgent>();

// State / storage (in-memory for the sample; production uses Blob / Cosmos).
builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddSingleton<ConversationState>();
builder.Services.AddSingleton<UserState>();

// Dialog.
builder.Services.AddSingleton<UserProfileDialog>();

// MSAL — outbound credentials read from the "Connections" section in appsettings.
builder.Services.AddDefaultMsalAuth(builder.Configuration);

// Inbound token validation — JWT bearer, configured from the "TokenValidation" section.
// In Development, appsettings.Development.json sets TokenValidation:Enabled=false so
// the Microsoft 365 Agents Playground / Bot Framework Emulator can post anonymously.
var tokenValidationEnabled = builder.Configuration
    .GetSection("TokenValidation")
    .GetValue("Enabled", true);

if (tokenValidationEnabled)
{
    builder.Services.AddAgentAspNetAuthentication(builder.Configuration);
    builder.Services.AddAuthorization();
}

var app = builder.Build();

if (tokenValidationEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// In dev (Microsoft 365 Agents Playground) skip RequireAuthorization so anonymous
// requests succeed. In Production, the JWT scheme above gates inbound traffic.
var messages = app.MapPost("/api/messages",
    async (HttpRequest req, HttpResponse res, IAgentHttpAdapter adapter, IAgent agent, CancellationToken ct) =>
    {
        await adapter.ProcessAsync(req, res, agent, ct);
    });

if (tokenValidationEnabled && !app.Environment.IsDevelopment())
{
    messages.RequireAuthorization();
}

app.Run();

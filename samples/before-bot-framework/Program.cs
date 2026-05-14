using MigrationSample.Before.Bots;
using MigrationSample.Before.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddControllers().AddNewtonsoftJson();

// Bot Framework — authentication and adapter wiring (LEGACY pattern).
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

// State / storage (in-memory for the sample; production uses Blob / Cosmos).
builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddSingleton<UserState>();
builder.Services.AddSingleton<ConversationState>();

// Dialog.
builder.Services.AddSingleton<UserProfileDialog>();

// Bot.
builder.Services.AddTransient<IBot, UserProfileBot>();

var app = builder.Build();

app.UseWebSockets();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();

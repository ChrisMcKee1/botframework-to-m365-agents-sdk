using System.Text.Json;
using AdaptiveCards.Templating;
using MigrationSample.After.Dialogs;
using MigrationSample.After.Models;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.Compat;
using Microsoft.Agents.Builder.Dialogs;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;

namespace MigrationSample.After.Agents;

public class UserProfileAgent : ActivityHandler
{
    private readonly ConversationState _conversationState;
    private readonly UserState _userState;
    private readonly UserProfileDialog _dialog;
    private readonly IStatePropertyAccessor<UserProfile> _profileAccessor;

    public UserProfileAgent(ConversationState conversationState, UserState userState, UserProfileDialog dialog)
    {
        _conversationState = conversationState;
        _userState = userState;
        _dialog = dialog;
        _profileAccessor = userState.CreateProperty<UserProfile>("UserProfile");
    }

    public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
    {
        await _conversationState.LoadAsync(turnContext, false, cancellationToken);
        await _userState.LoadAsync(turnContext, false, cancellationToken);

        await base.OnTurnAsync(turnContext, cancellationToken);

        await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
    }

    protected override async Task OnMessageActivityAsync(
        ITurnContext<IMessageActivity> turnContext,
        CancellationToken cancellationToken)
    {
        var profile = await _profileAccessor.GetAsync(turnContext, () => new UserProfile(), cancellationToken);

        if (string.IsNullOrEmpty(profile.Name))
        {
            // New API: pass ConversationState directly (not an IStatePropertyAccessor<DialogState>).
            await _dialog.RunAsync(turnContext, _conversationState, cancellationToken);
        }
        else
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Text($"{profile.Name} said: {turnContext.Activity.Text}"),
                cancellationToken);
        }
    }

    protected override async Task OnMembersAddedAsync(
        IList<ChannelAccount> membersAdded,
        ITurnContext<IConversationUpdateActivity> turnContext,
        CancellationToken cancellationToken)
    {
        var cardPath = Path.Combine(AppContext.BaseDirectory, "Cards", "welcomeCard.json");
        var cardTemplate = new AdaptiveCardTemplate(await File.ReadAllTextAsync(cardPath, cancellationToken));

        foreach (var member in membersAdded)
        {
            if (member.Id == turnContext.Activity.Recipient.Id) continue;

            var cardJson = cardTemplate.Expand(new { name = string.IsNullOrEmpty(member.Name) ? "there" : member.Name });
            var attachment = new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonSerializer.Deserialize<JsonElement>(cardJson),
            };
            await turnContext.SendActivityAsync(MessageFactory.Attachment(attachment), cancellationToken);
        }
    }
}

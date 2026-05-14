using AdaptiveCards.Templating;
using MigrationSample.Before.Dialogs;
using MigrationSample.Before.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace MigrationSample.Before.Bots;

public class UserProfileBot : ActivityHandler
{
    private readonly ConversationState _conversationState;
    private readonly UserState _userState;
    private readonly UserProfileDialog _dialog;
    private readonly IStatePropertyAccessor<UserProfile> _profileAccessor;
    private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;

    public UserProfileBot(ConversationState conversationState, UserState userState, UserProfileDialog dialog)
    {
        _conversationState = conversationState;
        _userState = userState;
        _dialog = dialog;
        _profileAccessor = userState.CreateProperty<UserProfile>("UserProfile");
        _dialogStateAccessor = conversationState.CreateProperty<DialogState>("DialogState");
    }

    public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
    {
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
            await _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
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
                Content = JsonConvert.DeserializeObject(cardJson),
            };
            await turnContext.SendActivityAsync(MessageFactory.Attachment(attachment), cancellationToken);
        }
    }
}

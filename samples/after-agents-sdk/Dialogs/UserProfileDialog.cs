using MigrationSample.After.Models;
using Microsoft.Agents.Builder.Dialogs;
using Microsoft.Agents.Builder.Dialogs.Prompts;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;

namespace MigrationSample.After.Dialogs;

public class UserProfileDialog : ComponentDialog
{
    private readonly IStatePropertyAccessor<UserProfile> _profileAccessor;

    public UserProfileDialog(UserState userState) : base(nameof(UserProfileDialog))
    {
        _profileAccessor = userState.CreateProperty<UserProfile>("UserProfile");

        var waterfall = new WaterfallStep[]
        {
            NameStepAsync,
            ConfirmStepAsync,
            SummaryStepAsync,
        };

        AddDialog(new WaterfallDialog("waterfall", waterfall));
        AddDialog(new TextPrompt("text"));
        AddDialog(new ConfirmPrompt("confirm"));

        InitialDialogId = "waterfall";
    }

    private static Task<DialogTurnResult> NameStepAsync(WaterfallStepContext step, CancellationToken ct)
        => step.PromptAsync(
            "text",
            new PromptOptions { Prompt = MessageFactory.Text("What's your name?") },
            ct);

    private static Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext step, CancellationToken ct)
    {
        step.Values["name"] = (string)step.Result;
        return step.PromptAsync(
            "confirm",
            new PromptOptions { Prompt = MessageFactory.Text($"Save '{step.Values["name"]}' as your name?") },
            ct);
    }

    private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext step, CancellationToken ct)
    {
        if ((bool)step.Result)
        {
            var profile = await _profileAccessor.GetAsync(step.Context, () => new UserProfile(), ct);
            profile.Name = (string)step.Values["name"];
            await _profileAccessor.SetAsync(step.Context, profile, ct);
            await step.Context.SendActivityAsync(
                $"Got it, {profile.Name}. Send another message and I'll echo it back.",
                cancellationToken: ct);
        }
        else
        {
            await step.Context.SendActivityAsync("OK — send a new message to try again.", cancellationToken: ct);
        }
        return await step.EndDialogAsync(cancellationToken: ct);
    }
}

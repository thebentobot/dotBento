using NetCord;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

public sealed class BentoSlashCommand(InteractiveService interactiveService, BentoCommand bentoCommand, UserSettingService userSettingService)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("bento", "Give a friend a Bento Box")]
    public async Task BentoCommand([SlashCommandParameter(Name = "user", Description = "Pick a User. Omit to check status")] User? user = null,
        [SlashCommandParameter(Name = "hide", Description = "Only show result for you")] bool? hide = null)
    {
        var effectiveHide = hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id);
        if (user == null)
        {
            await Context.SendResponse(interactiveService, await bentoCommand.CheckBentoCommand(Context.User), effectiveHide);
            return;
        }
        await user.ReturnIfBot(Context, interactiveService);
        await Context.SendResponse(interactiveService, await bentoCommand.GiveBentoCommand(Context.User, user), effectiveHide);
    }
}

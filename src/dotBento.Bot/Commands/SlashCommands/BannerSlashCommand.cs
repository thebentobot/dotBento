using NetCord;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Services;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

public sealed class BannerSlashCommand(
    InteractiveService interactiveService,
    BannerCommand bannerCommand,
    UserSettingService userSettingService,
    IDiscordUserResolver userResolver) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("banner", "Get the banner of a User Profile")]
    public async Task UserBannerCommand(
        [SlashCommandParameter(Name = "user", Description = "Pick a User")] User? user = null,
        [SlashCommandParameter(Name = "hide", Description = "Only show banner for you")] bool? hide = null
    )
    {
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var restUser = await userResolver.GetRestUserAsync(user.Id);
        var embed = await bannerCommand.Command(restUser);
        var ephemeral = embed.Embed.Color == new Color(0xFF0000) || (hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
        await Context.SendResponse(interactiveService, embed, ephemeral);
    }
}

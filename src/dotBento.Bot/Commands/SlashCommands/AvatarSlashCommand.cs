using NetCord;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Services;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[SlashCommand("avatar", "Get the avatar of a user")]
public sealed class AvatarSlashCommand(InteractiveService interactiveService, AvatarCommand avatarCommand, UserSettingService userSettingService, GuildMemberLookupService memberLookup) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("user", "Get the avatar of a User Profile")]
    public async Task UserAvatarCommand(
        [SlashCommandParameter(Name = "user", Description = "Pick a User")] User? user = null,
        [SlashCommandParameter(Name = "hide", Description = "Only show avatar for you")] bool? hide = null
        )
    {
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        await Context.SendResponse(interactiveService, await avatarCommand.UserAvatarCommand(user), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [GuildOnly]
    [SubSlashCommand("server", "Get the avatar of a Server Profile")]
    public async Task ServerAvatarCommand(
        [SlashCommandParameter(Name = "user", Description = "Pick a User")] GuildUser? user = null,
        [SlashCommandParameter(Name = "hide", Description = "Only show avatar for you")] bool? hide = null
    )
    {
        user ??= await memberLookup.GetOrFetchAsync(Context.Guild!.Id, Context.User.Id, Context.Guild);
        if (user is null)
        {
            var err = new dotBento.Bot.Models.Discord.ResponseModel { ResponseType = dotBento.Bot.Enums.ResponseType.Embed };
            err.Embed.WithTitle("User not found").WithDescription("Could not resolve the requested user.").WithColor(new Color(0xFF0000));
            await Context.SendResponse(interactiveService, err, true);
            return;
        }
        await user.ReturnIfBot(Context, interactiveService);
        await Context.SendResponse(interactiveService, await avatarCommand.ServerAvatarCommand(user), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }
}

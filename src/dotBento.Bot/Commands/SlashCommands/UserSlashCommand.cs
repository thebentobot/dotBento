using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using dotBento.Bot.Attributes;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Bot.Extensions;
using dotBento.Bot.Services;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;

namespace dotBento.Bot.Commands.SlashCommands;

[SlashCommand("user", "Commands for Discord Users")]
public sealed class UserSlashCommand(InteractiveService interactiveService, UserCommand userCommand, SettingsCommand settingsCommand, UserSettingService userSettingService, GuildMemberLookupService memberLookup)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("info", "Show info for a user")]
    public async Task InfoCommand([SlashCommandParameter(Name = "user", Description = "Pick a User")] User? user = null,
        [SlashCommandParameter(Name = "hide", Description = "Only show user info for you")] bool? hide = null)
    {
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        await Context.SendResponse(interactiveService, await userCommand.Command(user), hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [GuildOnly]
    [SubSlashCommand("profile", "Show a user's Bento profile")]
    public async Task ProfileCommand([SlashCommandParameter(Name = "user", Description = "Pick a User")] User? user = null,
        [SlashCommandParameter(Name = "hide", Description = "Only show user info for you")]
        bool? hide = null)
    {
        await Context.DeferResponseAsync();
        user ??= Context.User;
        await user.ReturnIfBot(Context, interactiveService);
        var guildMember = Context.Guild is not null
            ? await memberLookup.GetOrFetchAsync(Context.Guild.Id, user.Id, Context.Guild)
            : null;
        var botPfp = Context.Client.Cache.User?.GetAvatarUrl()?.ToString(1024);
        await Context.SendFollowUpResponse(interactiveService,
            await userCommand.GetProfileAsync((long)user.Id,
                (long)Context.Guild!.Id,
                guildMember,
                Context.Guild?.UserCount ?? 0,
                botPfp),
            hide ?? await userSettingService.ShouldHideCommandsAsync((long)Context.User.Id));
    }

    [SubSlashCommand("settings", "View and manage your personal settings")]
    public async Task SettingsCommand() =>
        await Context.SendResponse(interactiveService,
            await settingsCommand.GetUserSettingsAsync((long)Context.User.Id),
            true);
}

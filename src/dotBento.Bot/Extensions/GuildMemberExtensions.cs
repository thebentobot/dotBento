using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using dotBento.Bot.Services;

namespace dotBento.Bot.Extensions;

public static class GuildMemberExtensions
{
    public static Task<Result<Maybe<IGuildUser>>> ResolveMemberAsync(
        this SocketInteractionContext context, IDiscordGuildMemberResolver resolver, IUser user) =>
        context.Guild is { } guild
            ? resolver.ResolveAsync(guild, user.Id, user as IGuildUser)
            : Task.FromResult(Result.Success(Maybe<IGuildUser>.None));

    public static async Task<Maybe<IGuildUser>> RequireMemberAsync(
        this SocketInteractionContext context, IDiscordGuildMemberResolver resolver, IUser user)
    {
        var result = await context.ResolveMemberAsync(resolver, user);
        if (result.IsSuccess && result.Value.HasValue)
            return result.Value;

        var message = result.IsFailure ? result.Error : "That user is not in this server.";
        if (context.Interaction.HasResponded)
            await context.Interaction.FollowupAsync(message, ephemeral: true);
        else
            await context.Interaction.RespondAsync(message, ephemeral: true);
        return Maybe<IGuildUser>.None;
    }

    public static async Task<(string Name, string Avatar)> MemberAppearanceAsync(
        this SocketInteractionContext context, IDiscordGuildMemberResolver resolver, IUser user)
    {
        var result = await context.ResolveMemberAsync(resolver, user);
        var member = result.IsSuccess ? result.Value : Maybe<IGuildUser>.None;
        return Appearance(user, member);
    }

    internal static (string Name, string Avatar) Appearance(IUser user, Maybe<IGuildUser> member) =>
        member.HasValue
            ? (member.Value.Nickname ?? member.Value.DisplayName, member.Value.GetDisplayAvatarUrl())
            : (user.GlobalName ?? user.Username, user.GetDisplayAvatarUrl());
}

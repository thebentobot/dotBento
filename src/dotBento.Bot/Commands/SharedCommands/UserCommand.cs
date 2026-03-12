using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using dotBento.Bot.Enums;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services;
using dotBento.Infrastructure.Utilities;
using Microsoft.Extensions.Options;

namespace dotBento.Bot.Commands.SharedCommands;

public sealed class UserCommand(
    StylingUtilities stylingUtilities,
    ProfileCommands profileCommands,
    IOptions<BotEnvConfig> config,
    UserService userService)
{
    public async Task<ResponseModel> Command(User user)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var avatar = user.GetAvatarUrl()?.ToString(1024) ?? user.DefaultAvatarUrl.ToString(1024);
        var userPfpColour = await stylingUtilities.GetDominantColorAsync(avatar);
        embed.Embed
            .WithColor(userPfpColour)
            .WithTitle($"Profile for {user.GlobalName ?? user.Username}")
            .WithThumbnail(new EmbedThumbnailProperties(avatar))
            .AddFields([
                new EmbedFieldProperties().WithName("Username").WithValue(user.Username),
                new EmbedFieldProperties().WithName("User ID").WithValue(user.Id.ToString()),
                new EmbedFieldProperties().WithName("Account created on").WithValue($"<t:{user.CreatedAt.ToUnixTimeSeconds()}:F>"),
            ]);

        return embed;
    }

    public async Task<ResponseModel> GetProfileAsync(long userId, long guildId, GuildUser guildMember, int guildMemberCount, string botAvatarUrl)
    {
        var result = new ResponseModel
        {
            ResponseType = ResponseType.ImageOnly,
        };
        // Ensure the user exists in the database (creates if needed)
        await userService.CreateOrAddUserToCache(guildMember);
        var user = await userService.GetUserAsync((ulong)userId);
        if (user.HasNoValue)
        {
            result.ResponseType = ResponseType.Embed;
            result.Embed
                .WithColor(new Color(255, 0, 0))
                .WithTitle("Error")
                .WithDescription("Failed to create or retrieve user from database. Please try again later or contact support.");
            return result;
        }
        var imageServerHost = config.Value.ImageServer.Url;
        var lastFmApiKey = config.Value.LastFmApiKey;
        var profile =
            await profileCommands.GetProfileAsync(imageServerHost, lastFmApiKey, userId, guildId, guildMember, guildMemberCount, botAvatarUrl);

        if (profile.IsFailure)
        {
            result.ResponseType = ResponseType.Embed;
            result.Embed
                .WithColor(new Color(255, 0, 0))
                .WithTitle("Error")
                .WithDescription("Something went wrong while trying to get the profile. Please try again later. Feel free to contact support if the issue persists.");
            return result;
        }

        result.Stream = profile.Value;
        result.FileName = $"{guildMember.Username}_BentoProfile_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";

        return result;
    }
}

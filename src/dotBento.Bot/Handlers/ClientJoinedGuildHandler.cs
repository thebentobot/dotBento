using dotBento.Bot.Resources;
using dotBento.Domain;
using dotBento.Infrastructure.Services;
using NetCord.Gateway;
using NetCord.Rest;
using Serilog;
using DiscordGuild = NetCord.Gateway.Guild;

namespace dotBento.Bot.Handlers;

public sealed class ClientJoinedGuildHandler : IDisposable
{
    private readonly GatewayClient _client;
    private readonly GuildService _guildService;

    public ClientJoinedGuildHandler(GatewayClient client,
        GuildService guildService)
    {
        _client = client;
        _guildService = guildService;
        _client.GuildCreate += ClientJoinedGuildEvent;
    }

    private ValueTask ClientJoinedGuildEvent(GuildCreateEventArgs args)
    {
        if ((DateTimeOffset.UtcNow - args.Guild.JoinedAt).TotalSeconds > 30) return ValueTask.CompletedTask;
        _ = Task.Run(async () =>
        {
            try { await ClientJoinedGuild(args.Guild); }
            catch (Exception ex) { Log.Error(ex, "Unhandled exception in ClientJoinedGuild handler"); }
        });
        return ValueTask.CompletedTask;
    }

    private async Task ClientJoinedGuild(DiscordGuild guild)
    {
        Statistics.DiscordEvents.WithLabels(nameof(ClientJoinedGuild)).Inc();

        Log.Information(
            "JoinedGuild: {GuildName} / {GuildId} | {MemberCount} members",
            guild.Name, guild.Id, guild.UserCount);

        await _guildService.AddGuildAsync(guild);

        var embed = BuildWelcomeEmbed();

        try
        {
            var dm = await _client.Rest.GetDMChannelAsync(guild.OwnerId);
            await _client.Rest.SendMessageAsync(dm.Id, new MessageProperties { Embeds = [embed] });
        }
        catch (Exception)
        {
            Log.Information("Could not send message to guild owner for JoinedGuild {GuildName} / {GuildId}",
                guild.Name, guild.Id);
        }

        try
        {
            if (guild.SystemChannelId.HasValue)
            {
                await _client.Rest.SendMessageAsync(guild.SystemChannelId.Value, new MessageProperties { Embeds = [embed] });
            }
        }
        catch (Exception)
        {
            Log.Information("Could not send message to guild system channel for JoinedGuild {GuildName} / {GuildId}",
                guild.Name, guild.Id);
        }
    }

    private EmbedProperties BuildWelcomeEmbed()
    {
        var botUser = _client.Cache.User;
        var embed = new EmbedProperties()
            .WithTitle("Hello! My name is Bento 🍱")
            .WithDescription("Thank you for choosing me to service your server!")
            .WithFields([
                new EmbedFieldProperties()
                    .WithName("Check out the website for more information and help with all commands and settings")
                    .WithValue(DiscordConstants.WebsiteUrl),
                new EmbedFieldProperties()
                    .WithName("Need help? Or do you have some ideas or feedback to Bento 🍱? Feel free to join the support server!")
                    .WithValue("https://discord.gg/dd68WwP"),
                new EmbedFieldProperties()
                    .WithName("Want to check out the code for Bento?")
                    .WithValue("https://github.com/thebentobot/bento"),
                new EmbedFieldProperties()
                    .WithName("Want additional benefits when using Bento?")
                    .WithValue("https://www.patreon.com/bentobot"),
                new EmbedFieldProperties()
                    .WithName("Get a Bento 🍱 for each tip")
                    .WithValue("https://ko-fi.com/bentobot"),
            ])
            .WithFooter(new EmbedFooterProperties().WithText("Bento 🍱 is created by `banner.`"));

        return embed;
    }

    public void Dispose()
    {
        _client.GuildCreate -= ClientJoinedGuildEvent;
    }
}

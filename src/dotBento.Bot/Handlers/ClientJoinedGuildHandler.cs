using dotBento.Bot.Services;
using dotBento.Domain;
using Discord;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Infrastructure.Services;
using Serilog;

namespace dotBento.Bot.Handlers;

public sealed class ClientJoinedGuildHandler : IDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly GuildService _guildService;
    private readonly BotPresentation _presentation;

    public ClientJoinedGuildHandler(DiscordSocketClient client,
        GuildService guildService, BotPresentation? presentation = null)
    {
        _client = client;
        _presentation = presentation ?? new();
        _guildService = guildService;
        _client.JoinedGuild += ClientJoinedGuildEvent;
    }

    private Task ClientJoinedGuildEvent(SocketGuild guild)
    {
        _ = Task.Run(async () =>
        {
            try { await ClientJoinedGuild(guild); }
            catch (Exception ex) { Log.Error(ex, "Unhandled exception in ClientJoinedGuild handler"); }
        });
        return Task.CompletedTask;
    }

    private async Task ClientJoinedGuild(SocketGuild guild)
    {
        Statistics.DiscordEvents.WithLabels(nameof(ClientJoinedGuild)).Inc();

        Log.Information(
            "JoinedGuild: {GuildName} / {GuildId} | {MemberCount} members", guild.Name, guild.Id, guild.MemberCount);

        await _guildService.AddGuildAsync(guild);
        try
        {
            await guild.Owner.SendMessageAsync(embed: ResponseToNewGuild().Result.Embed.Build());
        }
        catch (Exception)
        {
            Log.Information("Could not send message to guild owner for JoinedGuild {GuildName} / {GuildId}\n",
                guild.Name, guild.Id);
        }

        try
        {
            await guild.SystemChannel.SendMessageAsync(embed: ResponseToNewGuild().Result.Embed.Build());
        }
        catch (Exception)
        {
            Log.Information("Could not send message to guild system channel for JoinedGuild {GuildName} / {GuildId}",
                guild.Name, guild.Id);
        }
    }

    private Task<ResponseModel> ResponseToNewGuild() =>
        Task.FromResult(BuildWelcomeEmbed(_client.CurrentUser.GetAvatarUrl(), _presentation));

    internal static ResponseModel BuildWelcomeEmbed(string botAvatarUrl, BotPresentation presentation)
    {
        var brand = presentation.Brand;
        var responseToGuildOwner = new ResponseModel { ResponseType = ResponseType.Embed };
        var embed = responseToGuildOwner.Embed;
        embed.WithColor(presentation.Primary)
            .WithAuthor(brand.Name, botAvatarUrl,
                string.IsNullOrWhiteSpace(brand.WebsiteUrl) ? null : brand.WebsiteUrl)
            .WithTitle($"Hello! My name is {brand.DisplayName}")
            .WithDescription("Thank you for choosing me to service your server!");
        var guide = BotPresentation.Link(brand.GetServerSettingsGuideUrl(),
            "Open the server settings guide");
        var settingsActions = presentation.HasWebsiteLeaderboard
            ? "control website leaderboard visibility and make commands disabled or admin-only"
            : "make commands disabled or admin-only";
        embed.AddField($"Configure {brand.Name} for your server",
            $"Run `/server settings` with the **Manage Server** permission to {settingsActions}.\n" + guide);
        BotPresentation.AddLink(embed, "Browse all commands and guides", brand.DocumentationUrl);
        BotPresentation.AddLink(embed, $"Visit the {brand.Name} website", brand.WebsiteUrl);
        BotPresentation.AddLink(embed, $"Need help or have feedback for {brand.DisplayName}? Join the support server!", brand.SupportUrl);
        BotPresentation.AddLink(embed, $"Want to check out the code for {brand.Name}?", brand.SourceUrl);
        if (presentation.Config.Features.PatreonEnabled)
            BotPresentation.AddLink(embed, $"Want additional benefits when using {brand.Name}?", brand.PatreonUrl);
        BotPresentation.AddLink(embed, presentation.DonationLinkTitle, brand.KoFiUrl);
        BotPresentation.AddLink(embed, presentation.VotingLinkTitle, brand.VoteUrl);
        return responseToGuildOwner;
    }

    public void Dispose()
    {
        _client.JoinedGuild -= ClientJoinedGuildEvent;
    }
}

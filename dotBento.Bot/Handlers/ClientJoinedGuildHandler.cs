using Discord;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Models;
using dotBento.Bot.Services;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace dotBento.Bot.Handlers;

public class ClientJoinedGuildHandler
{
    private readonly DiscordSocketClient _client;
    private readonly GuildService _guildService;
    
    public ClientJoinedGuildHandler(DiscordSocketClient client,
        GuildService guildService, IMemoryCache cache)
    {
        _client = client;
        _guildService = guildService;
        _client.JoinedGuild += ClientJoinedGuildEvent;
    }
    
    private Task ClientJoinedGuildEvent(SocketGuild guild)
    {
        _ = Task.Run(() => ClientJoinedGuild(guild));
        return Task.CompletedTask;
    }
    
    private async Task ClientJoinedGuild(SocketGuild guild)
    {
        Log.Information(
            "JoinedGuild: {guildName} / {guildId} | {memberCount} members", guild.Name, guild.Id, guild.MemberCount);
        
        await _guildService.AddGuildAsync(guild);
        try
        {
            await guild.Owner.SendMessageAsync(embed: this.ResponseToNewGuild(guild).Result.Embed.Build());
        }
        catch (Exception e)
        {
            Log.Information($"Could not send message to guild owner for JoinedGuild {guild.Name} / {guild.Id}",
                e.Message);
        }

        try
        {
            await guild.SystemChannel.SendMessageAsync(embed: this.ResponseToNewGuild(guild).Result.Embed.Build());
        }
        catch (Exception e)
        {
            Log.Information($"Could not send message to guild system channel for JoinedGuild {guild.Name} / {guild.Id}",
                e.Message);
        }
    }

    private Task<ResponseModel> ResponseToNewGuild(SocketGuild guild)
    {
        var responseToGuildOwner = new ResponseModel
        {
            ResponseType = ResponseType.Embed
        };
        responseToGuildOwner.EmbedAuthor
            .WithName(_client.CurrentUser.GlobalName)
            .WithUrl("https://www.bentobot.xyz/")
            .WithIconUrl(_client.CurrentUser.GetAvatarUrl());
        responseToGuildOwner.Embed.WithTitle("Hello! My name is Bento \ud83c\udf71");
        responseToGuildOwner.Embed.WithDescription("Thank you for choosing me to service your server!");
        responseToGuildOwner.Embed.AddField("Check out the website for more information and help with all commands and settings",
            "https://www.bentobot.xyz/");
        responseToGuildOwner.Embed.AddField("Need help? Or do you have some ideas or feedback to Bento \ud83c\udf71? Feel free to join the support server!",
            "https://discord.gg/dd68WwP");
        responseToGuildOwner.Embed.AddField("Want to check out the code for Bento?",
            "https://github.com/thebentobot/bento");
        responseToGuildOwner.Embed.AddField("Want additional benefits when using Bento?",
            "https://www.patreon.com/bentobot");
        responseToGuildOwner.Embed.AddField("Get a Bento \ud83c\udf71 for each tip",
            "https://ko-fi.com/bentobot");
        responseToGuildOwner.Embed.WithFooter("Vote on top.gg and receive 5 Bento \ud83c\udf71",
            "https://top.gg/bot/787041583580184609/vote");
        responseToGuildOwner.EmbedFooter
            .WithText("Bento \ud83c\udf71 is created by `banner.`")
            .WithIconUrl(_client.GetUser("232584569289703424").GetAvatarUrl());
        
        return Task.FromResult(responseToGuildOwner);
    }
}
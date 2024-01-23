using Discord;
using Discord.Commands;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Domain.Enums;
using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

namespace dotBento.Bot.TextCommands;

[Name("Ping")]
public class PingTextCommand(
    IOptions<BotEnvConfig> botSettings,
    BotDbContext botDbContext) : BaseCommandModule(botSettings)
{
    [Command("ping", RunMode = RunMode.Async)]
    [Summary("Test the Bento's latency")]
    public async Task PingCommand()
    {
        _ = Context.Channel.TriggerTypingAsync();

        var messageTimeStart = DateTime.UtcNow;
        var message = await ReplyAsync("\ud83c\udfd3 Pinging...");
        var messageTimeEnd = DateTime.UtcNow;
        var messageTime = Math.Round((messageTimeEnd - messageTimeStart).TotalMilliseconds);

        try
        {
            var dbTimeStart = DateTime.UtcNow;
            await botDbContext.Database.ExecuteSqlRawAsync("SELECT 1 + 1");
            var dbTimeEnd = DateTime.UtcNow;
            var dbTime = Math.Round((dbTimeEnd - dbTimeStart).TotalMilliseconds);

            var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
            embed.Embed.WithTitle("\ud83c\udfd3 Pong!")
                .WithDescription($"**Bento latency** {messageTime} ms\n**Discord latency** {Context.Client.Latency} ms\n**Database** {dbTime} ms")
                .WithColor(DiscordConstants.BentoYellow);
            await message.ModifyAsync(x => { x.Embed = embed.Embed.Build(); x.Content = null; });
        }
        catch (Exception e)
        {
            var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
            embed.Embed.WithTitle("\ud83c\udfd3 Pong!")
                .WithDescription($"**Bento latency** {messageTime} ms\n**Discord latency** {Context.Client.Latency} ms\n**Database** Connection was not established.")
                .WithColor(DiscordConstants.BentoYellow);
            await message.ModifyAsync(x => { x.Embed = embed.Embed.Build(); x.Content = null; });
            Context.LogCommandUsed(CommandResponse.Error);
            Log.Error(e, "Ping Slash Command failed");
        }
    }
}
using Discord;
using Discord.Interactions;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Domain.Enums;
using dotBento.EntityFramework.Context;
using Fergun.Interactive;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace dotBento.Bot.Commands.SlashCommands;

public class PingSlashCommand(BotDbContext botDbContext, InteractiveService interactiveService)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ping", "Test Bento's latency")]
    public async Task PingCommand(
    [Summary("hide", "Only show the result for you")] bool? hide = false)
    {
        var messageTimeStart = DateTime.UtcNow;
        var initialResponseEmbed = new ResponseModel{ ResponseType = ResponseType.Embed };
        initialResponseEmbed.Embed.WithTitle("\ud83c\udfd3 Pinging...");
        await Context.SendResponse(interactiveService, initialResponseEmbed, hide ?? false);
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
            await Context.SendFollowUpResponse(interactiveService, embed, hide ?? false);
        }
        catch (Exception e)
        {
            var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
            embed.Embed.WithTitle("\ud83c\udfd3 Pong!")
                .WithDescription($"**Bento latency** {messageTime} ms\n**Discord latency** {Context.Client.Latency} ms\n**Database** Connection was not established.")
                .WithColor(Color.Red);
            await Context.SendFollowUpResponse(interactiveService, embed, hide ?? false);
            Context.LogCommandUsed(CommandResponse.Error);
            Log.Error(e, "Ping Slash Command failed");
        }
    }
}
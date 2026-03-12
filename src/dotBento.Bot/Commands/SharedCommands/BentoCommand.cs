using CSharpFunctionalExtensions;
using NetCord;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Infrastructure.Services;

namespace dotBento.Bot.Commands.SharedCommands;

public sealed class BentoCommand(
    BentoService bentoService,
    SupporterService supporterService,
    UserService userService
    )
{
    public async Task<ResponseModel> GiveBentoCommand(User sender, User receiver)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        if (sender.Id == receiver.Id)
        {
            embed.Embed
                .WithColor(new Color(0xFF0000))
                .WithTitle("Sorry")
                .WithDescription("You're supposed to serve a Bento Box 🍱 to someone else, not yourself 🤨");
            return embed;
        }

        await userService.CreateOrAddUserToCache(sender);
        var bentoDataSender = await bentoService.FindBentoAsync((long)sender.Id);
        DateTime then;
        if (bentoDataSender.HasNoValue)
        {
            var bento = await bentoService.CreateBentoSenderAsync((long)sender.Id, DateTime.UtcNow.AddHours(-12));
            then = bento.BentoDate;
        }
        else
        {
            then = bentoDataSender.Value.BentoDate;
        }
        var now = DateTime.UtcNow;
        var difference = now - then;

        if (difference.TotalHours < 12)
        {
            var whenAgain = new DateTimeOffset(then.AddHours(12)).ToUnixTimeSeconds();
            var description = $"You can serve a Bento Box 🍱 to a friend again at <t:{whenAgain}:R>";
            embed.Embed
                .WithColor(DiscordConstants.BentoYellow)
                .WithTitle("Sorry \ud83d\ude14")
                .WithDescription(description);
            return embed;
        }

        await userService.CreateOrAddUserToCache(receiver);
        var (hasValue, patreon) = await supporterService.GetPatreonAsync((long)receiver.Id);
        var amount = 1;
        var bentoReceiverText = $"<@{receiver.Id}>";
        if (hasValue)
        {
            if (patreon.Follower)
            {
                amount = 2;
                bentoReceiverText = $"🌟 Official Patreon Bento 🍱 Follower 🌟 <@{receiver.Id}>";
            }
            else if (patreon.Enthusiast)
            {
                amount = 3;
                bentoReceiverText = $"🌟 Official Patreon Bento 🍱 Enthusiast 🌟 <@{receiver.Id}>";
            }
            else if (patreon.Disciple)
            {
                amount = 4;
                bentoReceiverText = $"🌟 Official Patreon Bento 🍱 Disciple 🌟 <@{receiver.Id}>";
            }
            else if (patreon.Sponsor)
            {
                amount = 5;
                bentoReceiverText = $"🌟 Official Patreon Bento 🍱 Sponsor 🌟 <@{receiver.Id}>";
            }
        }
        var bentoDataReceiver = await bentoService.UpsertBentoAsync((long)receiver.Id, amount);
        await bentoService.UpdateBentoDateAsync((long)sender.Id, now);

        embed.Embed
            .WithColor(DiscordConstants.BentoYellow)
            .WithDescription(
                $"<@{sender.Id}> just gave **{amount} Bento {(amount > 1 ? "Boxes" : "Box")} to {bentoReceiverText}**\n<@{receiver.Id}> has received **{bentoDataReceiver.Bento1} Bento {(bentoDataReceiver.Bento1 > 1 ? "Boxes" : "Box")}** over time \ud83d\ude0b\n<@{sender.Id}> can serve a Bento Box again <t:{new DateTimeOffset(now.AddHours(12)).ToUnixTimeSeconds()}:R>");

        return embed;
    }

    public async Task<ResponseModel> CheckBentoCommand(User user)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var bentoUser = await bentoService.FindBentoAsync((long)user.Id);
        if (bentoUser.HasNoValue)
        {
            embed.Embed
                .WithColor(DiscordConstants.BentoYellow)
                .WithDescription("You've never served a Bento to someone before \ud83d\ude33" +
                                 "\nTry it! Try to serve a Bento Box \ud83c\udf71 to a friend, I'm sure they'll be happy \u263a\ufe0f");
            return embed;
        }

        var bentoData = bentoUser.Value;
        var then = bentoData.BentoDate;
        var now = DateTime.UtcNow;
        var difference = now - then;
        if (difference.TotalHours < 12)
        {
            var whenAgain = new DateTimeOffset(then.AddHours(12)).ToUnixTimeSeconds();
            var description = $"<t:{whenAgain}:R> you can serve a Bento Box 🍱 to a friend again";
            embed.Embed
                .WithColor(DiscordConstants.BentoYellow)
                .WithDescription(description);
            return embed;
        }

        embed.Embed
            .WithColor(DiscordConstants.BentoYellow)
            .WithTitle("Ayy!")
            .WithDescription(
                "You can serve a friend a Bento Box \ud83c\udf71 again \ud83d\ude0b\nGo make someone's day!");
        return embed;
    }
}

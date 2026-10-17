using dotBento.Bot.Services;
using CSharpFunctionalExtensions;
using Discord;
using Discord.WebSocket;
using dotBento.Bot.Enums;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Resources;
using dotBento.Infrastructure.Services;

namespace dotBento.Bot.Commands.CommandHandlers;

public sealed class BentoCommand(
    BentoService bentoService,
    SupporterService supporterService,
    UserService userService,
    BotPresentation? presentation = null)
{
    private readonly BotPresentation _presentation = presentation ?? new();

    public async Task<ResponseModel> GiveBentoCommand(SocketUser sender, SocketUser receiver)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        if (sender.Id == receiver.Id)
        {
            embed.CommandResponse = dotBento.Domain.Enums.CommandResponse.WrongInput;
            embed.Embed
                .WithColor(_presentation.Error)
                .WithTitle("Sorry")
                .WithDescription($"You're supposed to serve a {_presentation.Brand.GiftName} {_presentation.Brand.GiftEmoji} to someone else, not yourself 🤨");
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
            embed.CommandResponse = dotBento.Domain.Enums.CommandResponse.Cooldown;
            var whenAgain = new DateTimeOffset(then.AddHours(12)).ToUnixTimeSeconds();
            var description = $"You can serve a {_presentation.Brand.GiftName} {_presentation.Brand.GiftEmoji} to a friend again at <t:{whenAgain}:R>";
            embed.Embed
                .WithColor(_presentation.Primary)
                .WithTitle("Sorry \ud83d\ude14")
                .WithDescription(description);
            return embed;
        }
        
        await userService.CreateOrAddUserToCache(receiver);
        var (hasValue, patreon) = await supporterService.GetPatreonAsync((long)receiver.Id);
        var amount = 1;
        var bentoReceiverText = $"{receiver.Mention}";
        if (hasValue)
        {
            if (patreon.Follower)
            {
                amount = 2;
                bentoReceiverText = $"🌟 Official Patreon {_presentation.Brand.Name} {_presentation.Brand.GiftEmoji} Follower 🌟 {receiver.Mention}";
            }
            else if (patreon.Enthusiast)
            {
                amount = 3;
                bentoReceiverText = $"🌟 Official Patreon {_presentation.Brand.Name} {_presentation.Brand.GiftEmoji} Enthusiast 🌟 {receiver.Mention}";
            }
            else if (patreon.Disciple)
            {
                amount = 4;
                bentoReceiverText = $"🌟 Official Patreon {_presentation.Brand.Name} {_presentation.Brand.GiftEmoji} Disciple 🌟 {receiver.Mention}";
            }
            else if (patreon.Sponsor)
            {
                amount = 5;
                bentoReceiverText = $"🌟 Official Patreon {_presentation.Brand.Name} {_presentation.Brand.GiftEmoji} Sponsor 🌟 {receiver.Mention}";
            }
        }
        var bentoDataReceiver = await bentoService.UpsertBentoAsync((long)receiver.Id, amount);
        await bentoService.UpdateBentoDateAsync((long)sender.Id, now);
        
        embed.Embed
            .WithColor(_presentation.Primary)
            .WithDescription(
                $"{sender.Mention} just gave **{amount} {_presentation.Brand.Gift(amount)} to {bentoReceiverText}**\n{receiver.Mention} has received **{bentoDataReceiver.Bento1} {_presentation.Brand.Gift(bentoDataReceiver.Bento1)}** over time 😋\n{sender.Mention} can serve a {_presentation.Brand.GiftName} again <t:{new DateTimeOffset(now.AddHours(12)).ToUnixTimeSeconds()}:R>");
        
        return embed;
    }

    public async Task<ResponseModel> CheckBentoCommand(SocketUser user)
    {
        var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
        var bentoUser = await bentoService.FindBentoAsync((long)user.Id);
        if (bentoUser.HasNoValue)
        {
            embed.Embed
                .WithColor(_presentation.Primary)
                .WithDescription($"You've never served a {_presentation.Brand.GiftName} to someone before \ud83d\ude33" +
                                 $"\nTry it! Try to serve a {_presentation.Brand.GiftName} {_presentation.Brand.GiftEmoji} to a friend, I'm sure they'll be happy \u263a\ufe0f");
            return embed;
        }
        
        var bentoData = bentoUser.Value;
        var then = bentoData.BentoDate;
        var now = DateTime.UtcNow;
        var difference = now - then;
        if (difference.TotalHours < 12)
        {
            embed.CommandResponse = dotBento.Domain.Enums.CommandResponse.Cooldown;
            var whenAgain = new DateTimeOffset(then.AddHours(12)).ToUnixTimeSeconds();
            var description = $"<t:{whenAgain}:R> you can serve a {_presentation.Brand.GiftName} {_presentation.Brand.GiftEmoji} to a friend again";
            embed.Embed
                .WithColor(_presentation.Primary)
                .WithDescription(description);
            return embed;
        }

        embed.Embed
            .WithColor(_presentation.Primary)
            .WithTitle("Ayy!")
            .WithDescription(
                $"You can serve a friend a {_presentation.Brand.GiftName} {_presentation.Brand.GiftEmoji} again \ud83d\ude0b\nGo make someone's day!");
        return embed;
    }
}

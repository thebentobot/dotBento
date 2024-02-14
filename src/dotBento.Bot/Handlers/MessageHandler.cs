using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dotBento.Bot.Attributes;
using dotBento.Bot.Enums;
using dotBento.Bot.Extensions;
using dotBento.Bot.Models.Discord;
using dotBento.Bot.Services;
using dotBento.Bot.Utilities;
using dotBento.Domain;
using dotBento.Domain.Enums;
using dotBento.Infrastructure.Interfaces;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;
using Microsoft.Extensions.Caching.Memory;
using Prometheus;
using Serilog;

namespace dotBento.Bot.Handlers;

public class MessageHandler
{
    private readonly DiscordSocketClient _client;
    private readonly IMemoryCache _cache;
    private readonly UserService _userService;
    private readonly GuildService _guildService;
    private readonly CommandService _commands;
    private readonly IPrefixService _prefixService;
    private readonly IServiceProvider _provider;
    private readonly InteractiveService _interactiveService;

    public MessageHandler(DiscordSocketClient client,
        IMemoryCache cache,
        UserService userService,
        GuildService guildService,
        CommandService commands,
        IPrefixService prefixService, 
        IServiceProvider provider,
        InteractiveService interactiveService)
    {
        _client = client;
        _cache = cache;
        _userService = userService;
        _guildService = guildService;
        _commands = commands;
        _prefixService = prefixService;
        _provider = provider;
        _interactiveService = interactiveService;
        _client.MessageReceived += MessageReceived;
    }

    private async Task MessageReceived(SocketMessage message)
    {
        Statistics.DiscordEvents.WithLabels(nameof(MessageReceived)).Inc();
        
        if (message is not SocketUserMessage msg)
        {
            return;
        }

        if (_client.CurrentUser != null && msg.Author?.Id == _client.CurrentUser?.Id)
        {
            return;
        }
        
        var context = new SocketCommandContext(_client, msg);
        
        if (context.User.IsBot)
        {
            return;
        }
        
        await _userService.CreateOrAddUserToCache(context.User);
        await _guildService.AddGuildMemberAsync(context.Guild.GetUser(context.User.Id));
        
        var patreonUser = await _userService.GetPatreonUserAsync(context.User.Id);

        await _userService.AddExperienceAsync(context, patreonUser);

        var messageArgumentPositionByIndex = 0;
        var prefix = _prefixService.GetPrefix(context.Guild?.Id);
        
        if (msg.HasStringPrefix(prefix, ref messageArgumentPositionByIndex, StringComparison.OrdinalIgnoreCase))
        {
            _ = Task.Run(() => ExecuteCommand(msg, context, messageArgumentPositionByIndex, prefix));
            return;
        }

        if (msg.HasMentionPrefix(_client.CurrentUser, ref messageArgumentPositionByIndex))
        {
            if (RegexPatterns.HasEmoteRegex.IsMatch(msg.Content))
            {
                Match emojiMatch;

                if ((emojiMatch = RegexPatterns.EmoteRegex.Match(msg.Content)).Success)
                {
                    var url = $"https://cdn.discordapp.com/emojis/{emojiMatch.Groups[1].Value}.png?v=1";
                    await msg.Channel.SendMessageAsync(url);
                    return;
                }

                if ((emojiMatch = RegexPatterns.AnimatedEmoteRegex.Match(msg.Content)).Success)
                {
                    var url = $"https://cdn.discordapp.com/emojis/{emojiMatch.Groups[1].Value}.gif?v=1";
                    await msg.Channel.SendMessageAsync(url);
                }
            }
            else
            {
                _ = Task.Run(() => ExecuteCommand(msg, context, messageArgumentPositionByIndex, prefix));
            }
        }
    }

    private async Task ExecuteCommand(SocketUserMessage msg, SocketCommandContext context, int argPosition, string prefix)
    {
        var searchResult = _commands.Search(context, argPosition);

        if ((searchResult.Commands == null || searchResult.Commands.Count == 0) && !msg.Content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        using (Statistics.TextCommandHandlerDuration.NewTimer())
        {
            if (searchResult.Commands == null || !searchResult.Commands.Any())
            {
                return;
            }

            if (searchResult.Commands[0].Command.Attributes.OfType<GuildOnly>().Any())
            {
                if (context.Guild == null)
                {
                    await context.User.SendMessageAsync("This command is not supported in DMs.");
                    context.LogCommandUsed(CommandResponse.NotSupportedInDm);
                    return;
                }
            }

            var commandName = searchResult.Commands[0].Command.Name;
            if (msg.Content.EndsWith(" help", StringComparison.OrdinalIgnoreCase) && commandName != "help")
            {
                var embed = new EmbedBuilder();
                var userName = (context.Message.Author as SocketGuildUser)?.DisplayName ??
                               context.User.GlobalName ?? context.User.Username;

                embed.HelpResponse(searchResult.Commands[0].Command, prefix, userName);
                await context.Channel.SendMessageAsync("", false, embed.Build());
                // TODO stats for help command
                return;
            }

            var result = await _commands.ExecuteAsync(context, argPosition, _provider);

            if (result.IsSuccess)
            {
                Statistics.CommandsExecuted.WithLabels(commandName).Inc();
            }
            else switch (result.Error)
            {
                case CommandError.ParseFailed:
                {
                    Statistics.CommandsFailed.WithLabels(commandName).Inc();
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                    embed.Embed.WithTitle("Error: Invalid input")
                        .WithDescription($"{result.ErrorReason}")
                        .WithColor(Color.Red);
                    await context.SendResponse(_interactiveService, embed);
                    break;
                }
                case CommandError.BadArgCount:
                {
                    Statistics.CommandsFailed.WithLabels(commandName).Inc();
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                    embed.Embed.WithTitle("Error: Bad argument count")
                        .WithDescription($"You have provided too many or too few arguments for the command `{commandName}`")
                        .WithColor(Color.Red);
                    await context.SendResponse(_interactiveService, embed);
                    break;
                }
                case CommandError.Exception:
                {
                    Statistics.CommandsFailed.WithLabels(commandName).Inc();
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                    embed.Embed.WithTitle("Error: Exception")
                        .WithDescription($"An exception occurred while executing the command `{commandName}`\nDon't worry, the developers have been notified and will fix it as soon as possible")
                        .WithColor(Color.Red);
                    await context.SendResponse(_interactiveService, embed);
                    break;
                }
                case CommandError.Unsuccessful:
                {
                    Statistics.CommandsFailed.WithLabels(commandName).Inc();
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                    embed.Embed.WithTitle("Error: Unsuccessful")
                        .WithDescription($"The command `{commandName}` was unsuccessful. Don't worry, the developers have been notified and will fix it as soon as possible")
                        .WithColor(Color.Red);
                    await context.SendResponse(_interactiveService, embed);
                    break;
                }
                case CommandError.ObjectNotFound:
                {
                    Statistics.CommandsFailed.WithLabels(commandName).Inc();
                    var embed = new ResponseModel{ ResponseType = ResponseType.Embed };
                    embed.Embed.WithTitle($"Error: {result.ErrorReason}")
                        .WithDescription($"The object was not found when attempting to run the command `{commandName}`.\nIf you believe this is an error, please contact the developers.\nYou can find the support server by running the command `about`")
                        .WithColor(Color.Red);
                    await context.SendResponse(_interactiveService, embed);
                    break;
                }
                // TODO would be nice to log when one of these errors below gets hit
                // ReSharper disable once RedundantCaseLabel
                case null:
                // ReSharper disable once RedundantCaseLabel
                case CommandError.UnknownCommand:
                // ReSharper disable once RedundantCaseLabel
                case CommandError.MultipleMatches:
                // ReSharper disable once RedundantCaseLabel
                case CommandError.UnmetPrecondition:
                default:
                    Log.Error("Command error: {Result}. Message content: {MessageContent}", result.ToString(), context.Message.Content);
                    Statistics.CommandsFailed.WithLabels(commandName).Inc();
                    break;
            }
        }
    }
    
    private bool CheckUserRateLimit(ulong discordUserId)
    {
        var cacheKey = $"{discordUserId}-rateLimit";
        if (_cache.TryGetValue(cacheKey, out int requestsInLastMinute))
        {
            if (requestsInLastMinute > 35)
            {
                return false;
            }

            requestsInLastMinute++;
            _cache.Set(cacheKey, requestsInLastMinute, TimeSpan.FromSeconds(60 - requestsInLastMinute));
        }
        else
        {
            _cache.Set(cacheKey, 1, TimeSpan.FromMinutes(1));
        }

        return true;
    }
}
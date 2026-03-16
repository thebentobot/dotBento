using NetCord.Gateway;
using NetCord.Services.Commands;
using dotBento.Bot.Commands.SharedCommands;
using dotBento.Domain;
using dotBento.Infrastructure.Interfaces;
using dotBento.Infrastructure.Services;
using Fergun.Interactive;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace dotBento.Bot.Handlers;

public sealed class MessageHandler : IDisposable
{
    private readonly GatewayClient _client;
    private readonly IMemoryCache _cache;
    private readonly UserService _userService;
    private readonly GuildService _guildService;
    private readonly CommandService<CommandContext> _commands;
    private readonly IPrefixService _prefixService;
    private readonly IServiceProvider _provider;
    private readonly InteractiveService _interactiveService;
    private readonly TagsCommand _tagsCommand;

    public MessageHandler(GatewayClient client,
        IMemoryCache cache,
        UserService userService,
        GuildService guildService,
        CommandService<CommandContext> commands,
        IPrefixService prefixService,
        IServiceProvider provider,
        InteractiveService interactiveService,
        TagsCommand tagsCommand)
    {
        _client = client;
        _cache = cache;
        _userService = userService;
        _guildService = guildService;
        _commands = commands;
        _prefixService = prefixService;
        _provider = provider;
        _interactiveService = interactiveService;
        _tagsCommand = tagsCommand;
        _client.MessageCreate += MessageReceived;
    }

    private ValueTask MessageReceived(Message msg)
    {
        Statistics.DiscordEvents.WithLabels(nameof(MessageReceived)).Inc();

        if (msg.Author.IsBot)
        {
            return ValueTask.CompletedTask;
        }

        _ = Task.Run(async () =>
        {
            try { await HandleMessageAsync(_client, msg); }
            catch (Exception ex) { Log.Error(ex, "Unhandled exception in message handler"); }
        });
        return ValueTask.CompletedTask;
    }

    private async Task HandleMessageAsync(GatewayClient client, Message msg)
    {
        var context = new CommandContext(msg, client);

        if (context.Guild == null || context.User.IsBot)
        {
            return;
        }

        await _guildService.AddGuildAsync(context.Guild);
        await _userService.CreateOrAddUserToCache(context.User);
        var guildMember = context.Guild.Users.GetValueOrDefault(context.User.Id);
        if (guildMember != null)
        {
            await _guildService.AddGuildMemberAsync(guildMember);
        }

        var patreonUser = await _userService.GetPatreonUserAsync(context.User.Id);

        await _userService.AddExperienceAsync(context.User.Id, context.Guild.Id, patreonUser);

        // TODO: Text commands are disabled because the bot does not have the MessageContent intent.
        // Without it, msg.Content is empty for guild messages. Re-enable the block below
        // (and commands.AddModules / prefixService.LoadAllPrefixes in BotService) when the intent is granted.
        /*
        var prefix = _prefixService.GetPrefix(context.Guild?.Id);

        // Check for string prefix
        if (msg.Content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var argPos = prefix.Length;
            _ = Task.Run(async () =>
            {
                try { await ExecuteCommand(msg, context, argPos, prefix); }
                catch (Exception ex) { Log.Error(ex, "Unhandled exception in text command execution"); }
            });
            return;
        }

        // Check for mention prefix
        var currentUserId = _client.Cache.User?.Id;
        if (currentUserId.HasValue)
        {
            var mentionPrefix = $"<@{currentUserId.Value}>";
            var mentionNickPrefix = $"<@!{currentUserId.Value}>";

            int? mentionArgPos = null;
            if (msg.Content.StartsWith(mentionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                mentionArgPos = mentionPrefix.Length;
            }
            else if (msg.Content.StartsWith(mentionNickPrefix, StringComparison.OrdinalIgnoreCase))
            {
                mentionArgPos = mentionNickPrefix.Length;
            }

            if (mentionArgPos.HasValue)
            {
                if (RegexPatterns.HasEmoteRegex.IsMatch(msg.Content))
                {
                    Match emojiMatch;

                    if ((emojiMatch = RegexPatterns.EmoteRegex.Match(msg.Content)).Success)
                    {
                        var url = $"https://cdn.discordapp.com/emojis/{emojiMatch.Groups[1].Value}.png?v=1";
                        await client.Rest.SendMessageAsync(msg.ChannelId, new MessageProperties().WithContent(url));
                        return;
                    }

                    if ((emojiMatch = RegexPatterns.AnimatedEmoteRegex.Match(msg.Content)).Success)
                    {
                        var url = $"https://cdn.discordapp.com/emojis/{emojiMatch.Groups[1].Value}.gif?v=1";
                        await client.Rest.SendMessageAsync(msg.ChannelId, new MessageProperties().WithContent(url));
                    }
                }
                else
                {
                    _ = Task.Run(async () =>
                    {
                        try { await ExecuteCommand(msg, context, mentionArgPos.Value, prefix); }
                        catch (Exception ex) { Log.Error(ex, "Unhandled exception in mention command execution"); }
                    });
                }
            }
        }
        */
    }

    /*
    // TODO: Re-enable when MessageContent intent is granted (see HandleMessageAsync above).
    private async Task ExecuteCommand(Message msg, CommandContext context, int argPosition, string prefix)
    {
        using (Statistics.TextCommandHandlerDuration.NewTimer())
        {
            var input = msg.Content[argPosition..];

            if (string.IsNullOrWhiteSpace(input) && !msg.Content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Check for " help" suffix before executing
            if (msg.Content.EndsWith(" help", StringComparison.OrdinalIgnoreCase))
            {
                // TODO stats for help command
                // Help embed logic removed (EmbedBuilder not available in NetCord migration)
                return;
            }

            var result = await _commands.ExecuteAsync(input.AsMemory(), context, _provider);

            if (result is not IFailResult failResult)
            {
                Statistics.CommandsExecuted.WithLabels("unknown").Inc();
            }
            else
            {
                // If the command was not found, attempt tag fallback
                if (failResult.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                    failResult.Message.Contains("unknown", StringComparison.OrdinalIgnoreCase))
                {
                    var tags = await _tagsCommand.FindTagAsync((long)context.Guild!.Id, msg.Content[prefix.Length..]);
                    await context.SendResponse(_interactiveService, tags);
                    return;
                }

                Statistics.CommandsFailed.WithLabels("unknown").Inc();
                Log.Error("Command error: {Result}. Message content: {MessageContent}", failResult.Message, msg.Content);

                var embed = new ResponseModel { ResponseType = ResponseType.Embed };
                embed.Embed.WithTitle("Error")
                    .WithDescription(failResult.Message);
                await context.SendResponse(_interactiveService, embed);
            }
        }
    }
    */

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

    public void Dispose()
    {
        _client.MessageCreate -= MessageReceived;
    }
}

using Discord.Interactions;
using dotBento.EntityFramework.Context;
using Serilog;

namespace dotBento.Bot.Modules;

public class CommandModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger _logger;
    private readonly BotDbContext _database;

    public CommandModule(ILogger logger, BotDbContext database)
    {
        _logger = logger;
        _database = database;
    }
    
    [SlashCommand("test", "Just a test command")]
    public async Task TestCommand()
    {
        _logger.Information("Test command called");
        var dbUser = _database.Users.FirstOrDefault(x => x.UserId == (long)this.Context.User.Id);
        if (dbUser == null)
        {
            await RespondAsync($"Hello There {this.Context.User.Username}. I could not find you in the database unfortunately");
        }
        else
        {
            await RespondAsync($"Hello There {dbUser.Username} you are level {dbUser.Level} and got {dbUser.Xp} xp");

        }
    }
}
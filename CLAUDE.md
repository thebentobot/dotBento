# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Test Commands

```bash
# Build entire solution
dotnet build dotBento.sln

# Run all tests
dotnet test dotBento.sln

# Run tests for a specific project
dotnet test tests/dotBento.Bot.Tests
dotnet test tests/dotBento.Infrastructure.Tests
dotnet test tests/dotBento.WebApi.Tests

# Run a single test by name
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Run the bot locally
dotnet run --project src/dotBento.Bot

# Run the web API locally
dotnet run --project src/dotBento.WebApi
```

## Architecture Overview

This is a Discord bot written in .NET 10 using Discord.NET, with a clean architecture separation:

### Projects

- **dotBento.Bot** - Discord bot entry point and presentation layer. Contains slash commands, text commands, handlers for Discord events, and DI configuration in `Startup.cs`.
- **dotBento.Infrastructure** - Business logic and external service integrations. Contains services, API clients, and command logic shared between slash/text commands.
- **dotBento.Domain** - Domain models, constants, and statistics tracking.
- **dotBento.EntityFramework** - EF Core database context (`BotDbContext`), entity definitions, and migrations for PostgreSQL.
- **dotBento.WebApi** - ASP.NET Core web API for public endpoints (profile data, etc). Uses API key middleware for auth.

### Command Pattern

Commands follow a two-layer pattern:
1. **SlashCommands/TextCommands** (in `dotBento.Bot/Commands/`) - Thin presentation layer that handles Discord interaction context, parameter parsing, and response formatting.
2. **SharedCommands** (in `dotBento.Bot/Commands/SharedCommands/`) - Reusable command logic shared between slash and text commands. These call services from Infrastructure.
3. **Infrastructure Commands** (in `dotBento.Infrastructure/Commands/`) - Complex command logic that involves multiple services (e.g., `LastFmCommands`, `GameCommands`).

### Key Services

Services in `dotBento.Infrastructure/Services/`:
- `UserService` - User CRUD and caching
- `GuildService` - Guild settings and member management
- `BentoService` - Core "bento box" gift feature
- `TagService` - Custom server tags
- `ReminderService` - User reminders with Hangfire
- API clients in `Services/Api/` for Last.fm, weather, urban dictionary, Sushii image server

### Handlers

Discord event handlers in `dotBento.Bot/Handlers/`:
- `InteractionHandler` - Routes slash commands, user commands, buttons, modals, autocomplete
- `MessageHandler` - Text command parsing with configurable prefix
- Guild/member lifecycle handlers for tracking joins/leaves

### Configuration

- Bot config: `src/dotBento.Bot/configs/config.json` or environment variables
- Web API config: `src/dotBento.WebApi/appsettings.json` or environment variables
- Required services: PostgreSQL, Valkey/Redis for distributed caching

### Database

Uses EF Core with PostgreSQL. The `BotDbContext` in `dotBento.EntityFramework/Context/BotDbContext.cs` defines all entity mappings. To create migrations:
1. Comment out the IConfiguration constructor in BotDbContext
2. Uncomment the hardcoded connection string
3. Run `dotnet ef migrations add MigrationName --project src/dotBento.EntityFramework`
4. Revert the changes

### Docker Development

```bash
# Start local development environment (Postgres, Valkey, Sushii image server)
docker compose -f src/docker-compose.dev.yml up
```

## Conventions

- Follows [Conventional Commits](https://www.conventionalcommits.org/) for commit messages
- Tests use xUnit v3 with Moq for mocking
- `ResponseModel` pattern for consistent command responses (embed, text, file, or paginated)

### Functional Programming with CSharpFunctionalExtensions

The codebase uses [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions) for safer null handling and error management. Follow these patterns:

**Use `Maybe<T>` instead of null checks:**
```csharp
// Good - using Maybe
public async Task<Maybe<User>> GetUserAsync(ulong userId)
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
    return user.AsMaybe(); // Returns Maybe<User>
}

// In calling code
var maybeUser = await GetUserAsync(userId);
if (maybeUser.HasValue)
{
    var user = maybeUser.Value;
    // Work with user
}
else
{
    // Handle missing user
}

// Bad - using null checks
if (user == null) { /* ... */ }
if (discordGuild != null) { /* ... */ }
```

**Use `Result` for operations that can fail:**
```csharp
// Good - using Result
public async Task<Result<string>> ProcessCommandAsync()
{
    if (condition)
        return Result.Failure<string>("Error message");

    return Result.Success("Success value");
}

// In calling code
var result = await ProcessCommandAsync();
if (result.IsSuccess)
{
    var value = result.Value;
}
else
{
    Log.Error(result.Error);
}
```

**Converting between Maybe and null:**
```csharp
// Entity to Maybe
var user = await db.Users.FirstOrDefaultAsync(...);
return user.AsMaybe();

// Maybe to value with default
var userId = maybeUser.HasValue ? maybeUser.Value.UserId : 0;
var userId = maybeUser.Unwrap(defaultValue);
```

**Key benefits:**
- Explicit handling of optional values - no surprise `NullReferenceException`
- Chainable operations with `Map`, `Bind`, `Match`
- Self-documenting code - return types show whether values are optional
- Consistency with existing service methods that return `Maybe<T>` and `Result<T>`

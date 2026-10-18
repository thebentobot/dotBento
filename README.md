# Bento

The Discord bot behind [Bento](https://bentobot.xyz), written in .NET 10 with Discord.NET. Uses PostgreSQL for storage, Valkey for shared caching, and a separate ASP.NET Core API for [bento-web](https://github.com/thebentobot/bento-web).

Command reference and guides: [docs.bentobot.xyz](https://docs.bentobot.xyz).

## Local development

Use the .NET SDK specified in [global.json](global.json), Docker Compose, and a Discord bot token.

From the repository root, start PostgreSQL, Valkey, and the image server:

```sh
docker compose -f src/docker-compose.dev.yml up -d
cp .env.example .env
```

In `.env`, set `Discord__Token` and change `Username=postgres` to `Username=username` in `PostgreSQL__ConnectionString` to match the development database. The example lists optional API keys for music, weather, and other integrations.

```sh
dotnet run --project src/dotBento.Bot
```

The bot applies database migrations on startup. Stop the services with:

```sh
docker compose -f src/docker-compose.dev.yml down
```

PostgreSQL data persists in a named Docker volume.

## Web API

Run the bot once to apply database migrations, then open a separate terminal:

```sh
cd src/dotBento.WebApi
cp ../../.env.webapi.example .env
```

In this `.env`, set `ApiKey`, add `Discord__Token`, and change `Username=postgres` to `Username=username` in `DatabaseConnectionString`.

```sh
dotnet run --no-launch-profile --urls http://localhost:5072
```

The API loads `.env` from its working directory. Clients send `ApiKey` in the `X-Api-Key` header. For bento-web, use `API_URL=http://localhost:5072` and the same key as `API_KEY`.

## Project layout

- `src/dotBento.Bot/`: slash commands, Discord events, and bot startup.
- `src/dotBento.Infrastructure/`: command logic, services, and external integrations.
- `src/dotBento.Domain/`: domain models and constants.
- `src/dotBento.EntityFramework/`: PostgreSQL entities, context, and migrations.
- `src/dotBento.WebApi/`: endpoints used by the website.
- `tests/`: xUnit tests for the bot, infrastructure, and API.

## Development commands

```sh
dotnet build dotBento.sln
dotnet test dotBento.sln
```

When changing slash commands, regenerate the manifest used by [bento-docs](https://github.com/thebentobot/bento-docs) and commit it:

```sh
dotnet run --project tools/dotBento.CommandDocs -- export --output docs/slash-commands.json
```

Use Conventional Commits for commit messages. See [CONTRIBUTING.md](.github/CONTRIBUTING.md) for the contribution workflow.

[Privacy policy](PRIVACYPOLICY.md) · [Terms of service](TOS.md)

Licensed under AGPL-3.0. See [LICENSE](LICENSE). Avatar illustration by [Dan](https://twitter.com/dannalanart).

## Branding and optional features

Bot configuration comes from environment variables, including the root `.env`
file. Restart the bot after changing settings. See [.env.example](.env.example)
for every branding, theme, pagination and deployment setting. The bot validates
presentation settings and command metadata before migrating the database or
logging into Discord.

To run without access to the private bento-media-server API, set:

```dotenv
MediaServer__Enabled=false
```

This removes `/media` and `/horoscope` from Discord and command-permission menus.
It also rejects interactions from old horoscope settings panels without changing
saved preferences or contacting the API. Other bot features still require their
normal dependencies, including PostgreSQL and Valkey. Both features remain
enabled when the setting is omitted, so existing deployments need no change.
When enabled with an empty server URL, these commands remain registered and
return an unavailable response. The API key remains optional for servers that
accept unauthenticated requests.

You can disable features independently while leaving the master switch enabled:

```dotenv
MediaServer__Enabled=true
Features__MediaEnabled=false
Features__HoroscopeEnabled=true
```

The master switch takes precedence. Disabling a feature preserves saved zodiac
signs and command permissions for later re-enabling. Registration replaces the
commands in the configured target on startup. If changing between guild and
global registration, remove obsolete registrations from the previous target
separately.

Patreon features are independently optional and enabled by default:

```dotenv
Features__PatreonEnabled=false
```

Set this in both the bot and Web API environments when running them separately.
It hides Patreon promotions, disables supporter gift/XP bonuses and profile
emote slots, reports zero active supporters, and returns an empty list from the
existing Patreon API endpoint. Stored supporter records and emote preferences
are preserved for re-enabling. The media-server master switch does not affect
Patreon. Ko-fi links remain independently configurable with `Branding__KoFiUrl`.

For example, a fork can use the following identity while keeping existing
commands and database contracts:

```dotenv
Branding__Name=Mochi
Branding__Description=A friendly entertainment bot.
Branding__Emoji=🍡
Branding__GiftName=Cookie
Branding__GiftPluralName=Cookies
Branding__GiftEmoji=🍪
Branding__GiftCommandName=cookie
Branding__AboutCommandName=mochi
Branding__GiftLeaderboardName=cookies
Branding__WebsiteUrl=https://example.com
Branding__DocumentationUrl=https://example.com/docs
Branding__SupportUrl=https://discord.gg/your-server
Branding__SourceUrl=https://github.com/your-org/your-fork
Branding__PatreonUrl=
Branding__KoFiUrl=
Branding__VoteUrl=
Theme__Primary=8B5CF6
MediaServer__Enabled=false
Bot__BaseServerId=0
Bot__CommandGuildId=123456789012345678
```

This registers `/cookie`, `/about mochi` and `/leaderboard cookies`.
Stored permission IDs remain `bento`, `about:bento` and `leaderboard:bento:*`;
menus display the configured names. Unrelated command names stay fixed. Command
segments accept 1–32 lowercase ASCII letters, digits, underscores or hyphens.
Names that collide with existing commands/groups are rejected. Discord account
names and avatars are configured through Discord itself.

Omitted branding settings retain Bento defaults. Explicitly empty link settings
hide their links; URLs must otherwise use HTTP or HTTPS. For example,
`Branding__WebsiteUrl=` removes website links, website-only server settings and
their controls, while keeping command permissions and command leaderboards.
Removing the variable entirely restores the Bento default. Gift naming changes
presentation only: amounts, cooldowns and stored data remain unchanged. Saved
profile descriptions and user-selected colours are preserved. The semantic
palette affects ordinary embeds; platform colours, extracted avatar/banner
colours and colour previews retain their meaning. Pagination defaults to Unicode
controls and accepts custom Discord emoji overrides.

`Bot__CommandGuildId=0` registers globally. Set your own guild ID for development;
Debug and Release builds follow the same setting. `Bot__BaseServerId` identifies
the support guild for profile membership badges; zero disables lookup, and
omitting it uses Bento's support guild. `Bot__KeepGuildDataOnLeave` accepts an
explicit boolean; when omitted, guild data is retained outside `production` and
deleted in `production`. Temporary keep-data flags still take precedence.
`Bot__Status` overrides the activity text; empty or omitted uses user/server counts.
Bot-list submissions identify the logged-in bot automatically.

The checked-in command manifest always uses Bento defaults with both private API
features enabled, independently of the developer's environment. Forks can export
a deployment-specific manifest using actual process environment variables:

```bash
dotnet run --project tools/dotBento.CommandDocs -- export --output /tmp/my-commands.json --use-environment
```

The exporter does not load `.env` automatically. With `--use-environment`,
configured invocations and feature switches affect the output; canonical IDs
remain stable. The flag also works with `check --input`.

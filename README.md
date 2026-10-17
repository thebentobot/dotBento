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

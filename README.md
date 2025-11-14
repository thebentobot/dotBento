# Bento
[![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-yellow.svg)](https://conventionalcommits.org)
[![Build dotBento](https://github.com/thebentobot/dotBento/actions/workflows/dotnet.yml/badge.svg)](https://github.com/thebentobot/dotBento/actions/workflows/dotnet.yml)
[![Discord Bots](https://top.gg/api/widget/servers/787041583580184609.svg?noavatar=true)](https://top.gg/bot/787041583580184609)

A Discord bot written in .NET. This repository is the improved .NET rewrite of [Bento](https://github.com/thebentobot/Bento).

## Features (high level)
- Slash commands and (configurable) prefix commands
- Modular command architecture with Discord.NET (Commands + Interactions)
- PostgreSQL database via Entity Framework Core
- Optional Prometheus metrics and Loki/Serilog logging
- Valkey caching between bot and web api instances
- Docker-ready deployment
- Separate Web API for public endpoints

## Tech stack
- .NET 9
- [Discord.NET](https://docs.discordnet.dev/)
- ASP.NET Core + EF Core (PostgreSQL)
- Serilog (with Grafana Loki sink) and optional Discord webhook logging
- Prometheus client for metrics

## Getting started

TBD soon™️.

## Relevant links
- The commit linting rules follows Conventional Commits. You can read about the linting rules specifically [here](https://github.com/conventional-changelog/commitlint/tree/master/%40commitlint/config-conventional)
- [Privacy Policy](./PRIVACYPOLICY.md)
- [Terms of Service](./TOS.md)

## Contributing
Pull requests are welcome! Please ensure your contributions include tests where appropriate and follow Conventional Commits for commit messages.

## Development
The bot is mainly developed by [Christian](https://github.com/banner4422).

Pull requests are very welcome if the features/changes make sense and are up to par in quality.

## License
This project is licensed under the AGPL-3.0 License. See the LICENSE file for details: ./LICENSE

The avatar illustration is done by [Dan](https://twitter.com/dannalanart).

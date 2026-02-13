using dotBento.EntityFramework.Context;
using dotBento.Infrastructure.Services;
using dotBento.Infrastructure.Services.Api;
using dotBento.WebApi.Dtos;
using dotBento.WebApi.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotBento.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class InformationController(
    ILogger<InformationController> logger,
    BotDbContext dbContext,
    LeaderboardService leaderboardService,
    DiscordApiService discordApiService) : ControllerBase
{
    [HttpGet("UsageStats")]
    public async Task<ActionResult<UsageStatsDto>> GetUsageStats()
    {
        var memberCount = await dbContext.Guilds
            .Where(x => x.MemberCount.HasValue)
            .SumAsync(x => x.MemberCount.Value);

        var serverCount = await dbContext.Guilds.CountAsync();

        var result = new UsageStatsDto(memberCount, serverCount);
        return Ok(result);
    }

    [HttpGet("Patreon")]
    public async Task<ActionResult<IEnumerable<PatreonUserDto>>> GetPatreon() =>
        Ok(await dbContext.Patreons.Select(p => p.ToPatreonUserDto()).ToListAsync());

    [HttpGet("Leaderboard")]
    public async Task<ActionResult<LeaderboardResponseDto>> GetLeaderboard()
    {
        var globalResult = await leaderboardService.GetGlobalXpLeaderboardAsync();
        if (globalResult.IsFailure)
            return StatusCode(500, globalResult.Error);

        return Ok(new LeaderboardResponseDto(
            GuildName: null,
            Icon: null,
            Users: globalResult.Value.Select(e => new LeaderboardUserDto(
                e.Rank, e.UserId, e.Level, e.Xp,
                e.Username ?? "", e.Discriminator ?? "", e.AvatarUrl ?? "")).ToList()
        ));
    }

    [HttpGet("Leaderboard/{guildId}/{userId}")]
    public async Task<ActionResult<LeaderboardResponseDto>> GetLeaderboardWithAccess(
        string guildId, string userId)
    {
        if (!long.TryParse(guildId, out var guildIdLong) || guildIdLong < 0)
            return BadRequest("Invalid guild ID");

        if (!long.TryParse(userId, out var userIdLong) || userIdLong < 0)
            return BadRequest("Invalid user ID");

        var guild = await dbContext.Guilds.FindAsync(guildIdLong);
        if (guild == null)
            return NotFound("Guild not found");

        // Step 1: Check if user is a guild member in the database
        var isGuildMember = await dbContext.GuildMembers
            .AnyAsync(gm => gm.GuildId == guildIdLong && gm.UserId == userIdLong);

        if (!isGuildMember)
        {
            // Step 2: Check if user exists in the bot database at all
            var userExists = await dbContext.Users.AnyAsync(u => u.UserId == userIdLong);
            if (!userExists)
                return StatusCode(403, new LeaderboardAccessDeniedDto("not_bot_user"));

            // Step 3: Verify membership via Discord API
            var discordResult = await discordApiService.GetGuildMemberAsync(
                (ulong)guildIdLong, (ulong)userIdLong);

            if (discordResult.IsFailure)
            {
                logger.LogWarning(
                    "Discord API error checking membership for user {UserId} in guild {GuildId}: {Error}",
                    userIdLong, guildIdLong, discordResult.Error);
                return StatusCode(502, new LeaderboardAccessDeniedDto("discord_api_error"));
            }

            if (!discordResult.Value)
                return StatusCode(403, new LeaderboardAccessDeniedDto("not_member"));
        }

        // User is authorized — return leaderboard data
        var serverResult = await leaderboardService.GetServerXpLeaderboardAsync(guildIdLong);
        if (serverResult.IsFailure)
            return StatusCode(500, serverResult.Error);

        return Ok(new LeaderboardResponseDto(
            GuildName: guild.GuildName,
            Icon: guild.Icon,
            Users: serverResult.Value.Select(e => new LeaderboardUserDto(
                e.Rank, e.UserId, e.Level, e.Xp,
                e.Username ?? "", e.Discriminator ?? "", e.AvatarUrl ?? "")).ToList()
        ));
    }
}

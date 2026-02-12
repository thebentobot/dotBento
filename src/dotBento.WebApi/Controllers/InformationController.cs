using dotBento.EntityFramework.Context;
using dotBento.Infrastructure.Services;
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
    LeaderboardService leaderboardService) : ControllerBase
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

    [HttpGet($"Leaderboard/{{guildId?}}")]
    public async Task<ActionResult<LeaderboardResponseDto>> GetLeaderboard(string? guildId)
    {
        if (string.IsNullOrEmpty(guildId))
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

        if (!long.TryParse(guildId, out var guildIdLong))
        {
            return BadRequest("Invalid guild ID");
        }

        var guild = await dbContext.Guilds.FindAsync(guildIdLong);
        if (guild == null)
        {
            return NotFound("Guild not found");
        }

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

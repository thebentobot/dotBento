using dotBento.EntityFramework.Context;
using dotBento.WebApi.Dtos;
using dotBento.WebApi.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotBento.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class InformationController(ILogger<InformationController> logger, BotDbContext dbContext) : ControllerBase
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
    public async Task<ActionResult<IEnumerable<LeaderboardUserDto>>> GetLeaderboard(string? guildId)
    {
        if (string.IsNullOrEmpty(guildId))
        {
            return Ok(new LeaderboardResponseDto(
                GuildName: null,
                Icon: null,
                Users: await GetGlobalLeaderboardAsync()
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

        return Ok(new LeaderboardResponseDto(
            GuildName: guild.GuildName,
            Icon: guild.Icon,
            Users: await GetGuildLeaderboardAsync(guildIdLong)
        ));
    }
    
    private async Task<List<LeaderboardUserDto>> GetGlobalLeaderboardAsync()
    {
        var users = await dbContext.Users
            .OrderByDescending(u => u.Level)
            .ThenByDescending(u => u.Xp)
            .Take(50)
            .ToListAsync();

        return users.Select((u, i) => u.ToLeaderboardUserDto(i)).ToList();
    }

    private async Task<List<LeaderboardUserDto>> GetGuildLeaderboardAsync(long guildId)
    {
        var members = await dbContext.GuildMembers
            .Where(gm => gm.GuildId == guildId)
            .Include(gm => gm.User)
            .OrderByDescending(gm => gm.Level)
            .ThenByDescending(gm => gm.Xp)
            .Take(50)
            .ToListAsync();

        return members.Select((gm, i) => gm.ToLeaderboardUserDto(i)).ToList();
    }
}
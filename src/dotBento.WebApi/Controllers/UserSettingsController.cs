using dotBento.Infrastructure.Services;
using dotBento.WebApi.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace dotBento.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class UserSettingsController(
    UserSettingService userSettingService) : ControllerBase
{
    [HttpGet("{userId}")]
    public async Task<ActionResult<UserSettingsDto>> GetUserSettings(string userId)
    {
        if (!long.TryParse(userId, out var userIdLong) || userIdLong < 0)
            return BadRequest("Invalid user ID");

        var setting = await userSettingService.GetOrCreateUserSettingAsync(userIdLong);

        return Ok(new UserSettingsDto(
            setting.HideSlashCommandCalls,
            setting.ShowOnGlobalLeaderboard));
    }

    [HttpPut("{userId}")]
    public async Task<ActionResult<UserSettingsDto>> UpdateUserSettings(
        string userId, [FromBody] UserSettingsUpdateRequest request)
    {
        if (!long.TryParse(userId, out var userIdLong) || userIdLong < 0)
            return BadRequest("Invalid user ID");

        var setting = await userSettingService.UpdateUserSettingAsync(userIdLong, s =>
        {
            if (request.HideSlashCommandCalls.HasValue)
                s.HideSlashCommandCalls = request.HideSlashCommandCalls.Value;
            if (request.ShowOnGlobalLeaderboard.HasValue)
                s.ShowOnGlobalLeaderboard = request.ShowOnGlobalLeaderboard.Value;
        });

        return Ok(new UserSettingsDto(
            setting.HideSlashCommandCalls,
            setting.ShowOnGlobalLeaderboard));
    }
}

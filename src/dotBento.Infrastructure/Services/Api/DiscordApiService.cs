using System.Net;
using CSharpFunctionalExtensions;

namespace dotBento.Infrastructure.Services.Api;

public class DiscordApiService(HttpClient httpClient)
{
    public virtual async Task<Result<bool>> GetGuildMemberAsync(ulong guildId, ulong userId)
    {
        try
        {
            using var response = await httpClient.GetAsync($"guilds/{guildId}/members/{userId}");

            if (response.IsSuccessStatusCode)
                return Result.Success(true);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return Result.Success(false);

            return Result.Failure<bool>(
                $"Discord API returned {(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<bool>($"Discord API request failed: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return Result.Failure<bool>("Discord API request timed out");
        }
    }
}

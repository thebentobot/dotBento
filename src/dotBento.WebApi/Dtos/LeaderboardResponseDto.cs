namespace dotBento.WebApi.Dtos;

public sealed record LeaderboardResponseDto(string? GuildName, string? Icon, List<LeaderboardUserDto> Users);
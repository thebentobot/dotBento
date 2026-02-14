namespace dotBento.WebApi.Dtos;

public sealed record UserSettingsUpdateRequest(bool? HideSlashCommandCalls, bool? ShowOnGlobalLeaderboard);

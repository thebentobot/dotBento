namespace dotBento.EntityFramework.Entities;

public partial class GuildSetting
{
    public long GuildId { get; set; }

    public bool LeaderboardPublic { get; set; }

    public string[] DisabledCommands { get; set; } = [];

    public string[] AdminOnlyCommands { get; set; } = [];

    public virtual Guild Guild { get; set; } = null!;
}

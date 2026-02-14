namespace dotBento.EntityFramework.Entities;

public partial class UserSetting
{
    public long UserId { get; set; }

    public bool HideSlashCommandCalls { get; set; }

    public bool ShowOnGlobalLeaderboard { get; set; }

    public virtual User User { get; set; } = null!;
}

namespace dotBento.EntityFramework.Entities;

public partial class GuildMember
{
    public long GuildMemberId { get; set; }

    public long UserId { get; set; }

    public long GuildId { get; set; }

    public int Xp { get; set; }

    public int Level { get; set; }

    public string? AvatarUrl { get; set; }

    public virtual Guild Guild { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

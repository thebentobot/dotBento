namespace dotBento.EntityFramework.Entities;

public partial class Patreon
{
    public int Id { get; set; }

    public long UserId { get; set; }

    public string? Name { get; set; }

    public string? Avatar { get; set; }

    public bool Supporter { get; set; }

    public bool Follower { get; set; }

    public bool Enthusiast { get; set; }

    public bool Disciple { get; set; }

    public bool Sponsor { get; set; }

    public string? EmoteSlot1 { get; set; }

    public string? EmoteSlot2 { get; set; }

    public string? EmoteSlot3 { get; set; }

    public string? EmoteSlot4 { get; set; }

    public virtual User User { get; set; } = null!;
}

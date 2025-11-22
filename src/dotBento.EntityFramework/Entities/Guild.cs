namespace dotBento.EntityFramework.Entities;

public partial class Guild
{
    public long GuildId { get; set; }

    public string GuildName { get; set; } = null!;

    public string Prefix { get; set; } = null!;

    public bool Tiktok { get; set; }

    public bool Leaderboard { get; set; }

    public bool Media { get; set; }

    public string? Icon { get; set; }

    public int? MemberCount { get; set; }

    public virtual ICollection<GuildMember> GuildMembers { get; set; } = new List<GuildMember>();

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}

namespace dotBento.EntityFramework.Entities;

public partial class User
{
    public long UserId { get; set; }

    public string Discriminator { get; set; } = null!;

    public int Xp { get; set; }

    public int Level { get; set; }

    public string? Username { get; set; }

    public string? AvatarUrl { get; set; }

    public virtual Bento? Bento { get; set; }

    public virtual ICollection<GuildMember> GuildMembers { get; set; } = new List<GuildMember>();
    
    public virtual Lastfm? Lastfm { get; set; }
    
    public virtual Patreon? Patreon { get; set; }

    public virtual Profile? Profile { get; set; }

    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

    public virtual RpsGame? RpsGame { get; set; }

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();

    public virtual Weather? Weather { get; set; }
}

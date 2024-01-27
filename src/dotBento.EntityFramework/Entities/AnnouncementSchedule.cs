namespace dotBento.EntityFramework.Entities;

public partial class AnnouncementSchedule
{
    public int Id { get; set; }

    public long GuildId { get; set; }

    public long ChannelId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime Date { get; set; }

    public virtual Guild Guild { get; set; } = null!;
}

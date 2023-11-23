using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class AnnouncementTime
{
    public int Id { get; set; }

    public long GuildId { get; set; }

    public long ChannelId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime Date { get; set; }

    public int AmountOfTime { get; set; }

    public string Timeframe { get; set; } = null!;

    public virtual Guild Guild { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class Ban
{
    public long BanCase { get; set; }

    public long UserId { get; set; }

    public long GuildId { get; set; }

    public DateTime Date { get; set; }

    public string? Note { get; set; }

    public long? Actor { get; set; }

    public string? Reason { get; set; }

    public virtual Guild Guild { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class Tag
{
    public long TagId { get; set; }

    public long UserId { get; set; }

    public long GuildId { get; set; }

    public DateTime? Date { get; set; }

    public string Command { get; set; } = null!;

    public string Content { get; set; } = null!;

    public int Count { get; set; }

    public virtual Guild Guild { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class NotificationMessage
{
    public int Id { get; set; }

    public long UserId { get; set; }

    public long GuildId { get; set; }

    public string Content { get; set; } = null!;

    public bool? Global { get; set; }

    public virtual User User { get; set; } = null!;
}

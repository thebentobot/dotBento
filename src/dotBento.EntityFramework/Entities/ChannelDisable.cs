using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class ChannelDisable
{
    public int Id { get; set; }

    public long GuildId { get; set; }

    public long ChannelId { get; set; }

    public virtual Guild Guild { get; set; } = null!;
}

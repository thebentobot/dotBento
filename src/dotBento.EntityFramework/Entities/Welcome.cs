using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class Welcome
{
    public long GuildId { get; set; }

    public string? Message { get; set; }

    public long? Channel { get; set; }

    public virtual Guild Guild { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class RoleMessage
{
    public long GuildId { get; set; }

    public long? MessageId { get; set; }

    public string? Message { get; set; }

    public virtual Guild Guild { get; set; } = null!;
}

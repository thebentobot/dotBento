using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class CaseGlobal
{
    public long GuildId { get; set; }

    public bool ServerName { get; set; }

    public bool Reason { get; set; }

    public virtual Guild Guild { get; set; } = null!;
}

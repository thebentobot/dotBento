using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class MuteRole
{
    public long GuildId { get; set; }

    public long RoleId { get; set; }

    public virtual Guild Guild { get; set; } = null!;
}

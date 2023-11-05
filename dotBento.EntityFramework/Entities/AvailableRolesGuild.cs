using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class AvailableRolesGuild
{
    public string Role { get; set; } = null!;

    public int Id { get; set; }

    public long GuildId { get; set; }

    public virtual Guild Guild { get; set; } = null!;
}

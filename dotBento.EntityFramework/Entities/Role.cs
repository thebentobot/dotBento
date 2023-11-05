using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class Role
{
    public int Id { get; set; }

    public long RoleId { get; set; }

    public string RoleCommand { get; set; } = null!;

    public string? RoleName { get; set; }

    public long GuildId { get; set; }

    public virtual Guild Guild { get; set; } = null!;
}

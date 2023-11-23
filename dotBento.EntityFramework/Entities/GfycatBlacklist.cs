using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class GfycatBlacklist
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;
}

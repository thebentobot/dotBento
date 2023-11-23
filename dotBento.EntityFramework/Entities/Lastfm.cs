using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class Lastfm
{
    public long UserId { get; set; }

    public string Lastfm1 { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

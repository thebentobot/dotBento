using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class Horoscope
{
    public long UserId { get; set; }

    public virtual User User { get; set; } = null!;
}

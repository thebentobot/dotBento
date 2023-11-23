using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class Bento
{
    public long UserId { get; set; }

    public int Bento1 { get; set; }

    public DateTime BentoDate { get; set; }

    public virtual User User { get; set; } = null!;
}

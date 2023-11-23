using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class Weather
{
    public long UserId { get; set; }

    public string City { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

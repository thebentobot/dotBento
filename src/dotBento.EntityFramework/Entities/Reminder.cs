using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class Reminder
{
    public int Id { get; set; }

    public long UserId { get; set; }

    public DateTime Date { get; set; }

    public string Reminder1 { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

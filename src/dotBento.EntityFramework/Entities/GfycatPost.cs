using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class GfycatPost
{
    public int Id { get; set; }

    public long MessageId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime Date { get; set; }
}

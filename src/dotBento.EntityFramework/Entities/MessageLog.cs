﻿using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class MessageLog
{
    public long GuildId { get; set; }

    public long Channel { get; set; }

    public virtual Guild Guild { get; set; } = null!;
}

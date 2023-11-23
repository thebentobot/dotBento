using System.ComponentModel.DataAnnotations.Schema;
using dotBento.EntityFramework.Enums;

namespace dotBento.EntityFramework.Entities;

public partial class AvailableRolesGuild
{
    public string Role { get; set; } = null!;

    public int Id { get; set; }

    public long GuildId { get; set; }

    public virtual Guild Guild { get; set; } = null!;
    
    [Column("type")]
    public RoleType Type { get; set; }
}

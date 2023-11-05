using System.ComponentModel.DataAnnotations.Schema;
using dotBento.EntityFramework.Enums;

namespace dotBento.EntityFramework.Entities;

[Table("horoscope")]
public partial class Horoscope
{
    public long UserId { get; set; }

    public virtual User User { get; set; } = null!;
    
    [Column("horoscope")]
    public HoroscopeSign HoroscopeSign { get; set; }
}

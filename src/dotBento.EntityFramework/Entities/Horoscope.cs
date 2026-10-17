namespace dotBento.EntityFramework.Entities;

public partial class Horoscope
{
    public long UserId { get; set; }

    public string Sign { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

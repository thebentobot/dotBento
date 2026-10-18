namespace dotBento.EntityFramework.Entities;

public partial class UserOptOut
{
    public long UserId { get; set; }

    public DateTime OptedOutAt { get; set; }
}

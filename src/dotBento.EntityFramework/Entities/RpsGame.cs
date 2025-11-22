namespace dotBento.EntityFramework.Entities;

public partial class RpsGame
{
    public int Id { get; set; }

    public long UserId { get; set; }

    public int? PaperWins { get; set; }

    public int? PaperLosses { get; set; }

    public int? RockWins { get; set; }

    public int? RockLosses { get; set; }

    public int? ScissorWins { get; set; }

    public int? ScissorsLosses { get; set; }

    public int? PaperTies { get; set; }

    public int? RockTies { get; set; }

    public int? ScissorsTies { get; set; }

    public virtual User User { get; set; } = null!;
}

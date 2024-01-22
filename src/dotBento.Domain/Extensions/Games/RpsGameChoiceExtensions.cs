using dotBento.Domain.Enums.Games;

namespace dotBento.Domain.Extensions.Games;

public static class RpsGameChoiceExtensions
{
    public static string AddEmoji(this RpsGameChoice choice)
    {
        return choice switch
        {
            RpsGameChoice.Rock => "Rock 🪨",
            RpsGameChoice.Paper => "Paper 📄",
            RpsGameChoice.Scissors => "Scissors ✂️",
            _ => throw new ArgumentOutOfRangeException(nameof(choice), choice, null)
        };
    }
}
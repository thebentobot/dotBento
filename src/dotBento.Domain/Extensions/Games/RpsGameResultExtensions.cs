using dotBento.Domain.Enums.Games;

namespace dotBento.Domain.Extensions.Games;

public static class RpsGameResultExtensions
{
    public static string FormatResult(this RpsGameResult result)
    {
        return result switch
        {
            RpsGameResult.Win => "You **Win** 🎉",
            RpsGameResult.Loss => "You **Lose** 💀",
            RpsGameResult.Draw => "It's a **Draw** 🤝",
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
        };
    }
}
namespace dotBento.Infrastructure.Commands.Profile;

/// <summary>
/// Calculates layout properties based on which boards are enabled
/// </summary>
public static class ProfileLayoutCalculator
{
    public record LayoutResult(
        int FmOpacity,
        int XpOpacity,
        string DescriptionHeight,
        string FmPaddingTop
    );

    /// <summary>
    /// Calculates layout properties based on which boards (LastFM, XP) are visible
    /// </summary>
    /// <param name="hasLastFmBoard">Whether the Last.fm board is displayed</param>
    /// <param name="hasXpBoard">Whether the XP board is displayed</param>
    /// <returns>Layout configuration for the profile</returns>
    public static LayoutResult CalculateLayout(bool hasLastFmBoard, bool hasXpBoard)
    {
        return (hasLastFmBoard, hasXpBoard) switch
        {
            // Both boards hidden - maximize description space
            (false, false) => new LayoutResult(
                FmOpacity: 0,
                XpOpacity: 0,
                DescriptionHeight: "365px",
                FmPaddingTop: "32.5px"
            ),

            // Only XP board visible - hide FM, expand description moderately
            (false, true) => new LayoutResult(
                FmOpacity: 0,
                XpOpacity: 100,
                DescriptionHeight: "310px",
                FmPaddingTop: "88px"
            ),

            // Only LastFM board visible - hide XP, expand description moderately, push FM down
            (true, false) => new LayoutResult(
                FmOpacity: 100,
                XpOpacity: 0,
                DescriptionHeight: "310px",
                FmPaddingTop: "88px"
            ),

            // Both boards visible - standard layout
            (true, true) => new LayoutResult(
                FmOpacity: 100,
                XpOpacity: 100,
                DescriptionHeight: "250px",
                FmPaddingTop: "32.5px"
            )
        };
    }
}

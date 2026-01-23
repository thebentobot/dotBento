using dotBento.Infrastructure.Commands.Profile;

namespace dotBento.Infrastructure.Tests.Commands.Profile;

public class ProfileLayoutCalculatorTests
{
    [Fact]
    public void CalculateLayout_BothBoardsEnabled_ShouldReturnStandardLayout()
    {
        // Arrange
        const bool hasLastFm = true;
        const bool hasXp = true;

        // Act
        var result = ProfileLayoutCalculator.CalculateLayout(hasLastFm, hasXp);

        // Assert
        Assert.Equal(100, result.FmOpacity);
        Assert.Equal(100, result.XpOpacity);
        Assert.Equal("250px", result.DescriptionHeight);
        Assert.Equal("32.5px", result.FmPaddingTop);
    }

    [Fact]
    public void CalculateLayout_BothBoardsDisabled_ShouldMaximizeDescriptionSpace()
    {
        // Arrange
        const bool hasLastFm = false;
        const bool hasXp = false;

        // Act
        var result = ProfileLayoutCalculator.CalculateLayout(hasLastFm, hasXp);

        // Assert
        Assert.Equal(0, result.FmOpacity);
        Assert.Equal(0, result.XpOpacity);
        Assert.Equal("365px", result.DescriptionHeight);
        Assert.Equal("32.5px", result.FmPaddingTop);
    }

    [Fact]
    public void CalculateLayout_OnlyXpBoardEnabled_ShouldHideLastFmAndExpandDescription()
    {
        // Arrange
        const bool hasLastFm = false;
        const bool hasXp = true;

        // Act
        var result = ProfileLayoutCalculator.CalculateLayout(hasLastFm, hasXp);

        // Assert
        Assert.Equal(0, result.FmOpacity);
        Assert.Equal(100, result.XpOpacity);
        Assert.Equal("310px", result.DescriptionHeight);
        Assert.Equal("88px", result.FmPaddingTop);
    }

    [Fact]
    public void CalculateLayout_OnlyLastFmBoardEnabled_ShouldHideXpExpandDescriptionAndPushLastFmDown()
    {
        // Arrange
        const bool hasLastFm = true;
        const bool hasXp = false;

        // Act
        var result = ProfileLayoutCalculator.CalculateLayout(hasLastFm, hasXp);

        // Assert
        Assert.Equal(100, result.FmOpacity);
        Assert.Equal(0, result.XpOpacity);
        Assert.Equal("310px", result.DescriptionHeight);
        Assert.Equal("88px", result.FmPaddingTop); // This was the bug fix - LastFM should be pushed down
    }
}

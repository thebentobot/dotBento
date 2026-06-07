using dotBento.Infrastructure.Commands;

namespace dotBento.Infrastructure.Tests.Commands;

public sealed class GameCommandsTests
{
    [Fact]
    public void Roll_IncludesUpperBound()
    {
        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(5, GameCommands.Roll(5, 5));
        }
    }
}

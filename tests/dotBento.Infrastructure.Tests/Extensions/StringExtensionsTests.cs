using dotBento.Domain.Extensions;

namespace dotBento.Infrastructure.Tests.Extensions;

public sealed class StringExtensionsTests
{
    [Fact]
    public void ReplaceInvalidChars_PreservesFileExtension()
    {
        var result = "colour.png".ReplaceInvalidChars();

        Assert.Equal("colour.png", result);
    }

    [Fact]
    public void ReplaceInvalidChars_ReplacesUnsafeCharacters()
    {
        var result = "bad file'name/slash.png".ReplaceInvalidChars();

        Assert.Equal("bad_file_name_slash.png", result);
    }
}

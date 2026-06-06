using dotBento.Bot.Attributes;

namespace dotBento.Bot.Tests.Attributes;

public sealed class CommandMetadataAttributeTests
{
    [Fact]
    public void ExamplesAttribute_StoresExamples()
    {
        var attribute = new ExamplesAttribute("tag hello", "tag delete hello");

        Assert.Equal(["tag hello", "tag delete hello"], attribute.Examples);
    }

    [Fact]
    public void OptionsAttribute_StoresOptions()
    {
        var attribute = new OptionsAttribute("create", "delete");

        Assert.Equal(["create", "delete"], attribute.Options);
    }
}

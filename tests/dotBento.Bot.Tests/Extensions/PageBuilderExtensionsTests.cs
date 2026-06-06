using Discord;
using dotBento.Bot.Extensions;
using Fergun.Interactive;

namespace dotBento.Bot.Tests.Extensions;

public sealed class PageBuilderExtensionsTests
{
    [Fact]
    public void BuildStaticPaginator_BuildsSinglePagePaginator()
    {
        var paginator = new List<PageBuilder> { new PageBuilder().WithDescription("one") }
            .BuildStaticPaginator();

        Assert.Single(paginator.Pages);
    }

    [Fact]
    public void BuildStaticPaginator_AddsCustomOption()
    {
        var paginator = new List<PageBuilder> { new PageBuilder().WithDescription("one") }
            .BuildStaticPaginator("custom", new Emoji("✅"));

        Assert.Single(paginator.Pages);
    }

    [Fact]
    public void BuildStaticPaginator_AddsJumpOptionForLargePageSet()
    {
        var pages = Enumerable.Range(0, 25)
            .Select(index => new PageBuilder().WithDescription($"page {index}"))
            .ToList();

        var paginator = pages.BuildStaticPaginator();

        Assert.Equal(25, paginator.Pages.Count);
    }

    [Fact]
    public void BuildStaticPaginatorWithSelectMenu_BuildsPaginator()
    {
        var pages = Enumerable.Range(0, 10)
            .Select(index => new PageBuilder().WithDescription($"page {index}"))
            .ToList();
        var menu = new SelectMenuBuilder()
            .WithCustomId("menu")
            .AddOption("One", "one");

        var paginator = pages.BuildStaticPaginatorWithSelectMenu(menu);

        Assert.Equal(10, paginator.Pages.Count);
    }
}

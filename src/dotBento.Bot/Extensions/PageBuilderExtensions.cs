using dotBento.Bot.Resources;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using NetCord;
using NetCord.Rest;

namespace dotBento.Bot.Extensions;

public static class PageBuilderExtensions
{
    public static StaticPaginator BuildStaticPaginator(this IList<PageBuilder> pages, string? customOptionId = null, EmojiProperties? optionEmote = null)
    {
        var builder = new StaticPaginatorBuilder()
            .WithPages(pages)
            .WithFooter(PaginatorFooter.None)
            .WithActionOnTimeout(ActionOnStop.DeleteInput);

        if (pages.Count != 1 || customOptionId != null)
        {
            builder.WithOptions(DiscordConstants.PaginationEmotes);
        }

        if (customOptionId != null && optionEmote != null)
        {
            builder.AddOption(customOptionId, optionEmote, null, ButtonStyle.Primary);
        }

        if (customOptionId == null && pages.Count >= 25)
        {
            builder.AddOption(EmojiProperties.Custom(DiscordConstants.PagesGoTo), PaginatorAction.Jump);
        }

        return builder.Build();
    }

    public static StaticPaginator BuildStaticPaginatorWithSelectMenu(this IList<PageBuilder> pages,
        StringMenuProperties selectMenuBuilder)
    {
        var builder = new StaticPaginatorBuilder()
            .WithPages(pages)
            .WithFooter(PaginatorFooter.None)
            .WithActionOnTimeout(ActionOnStop.DeleteInput);

        if (pages.Count != 1)
        {
            builder.WithOptions(DiscordConstants.PaginationEmotes);
        }

        if (pages.Count >= 10)
        {
            builder.AddOption(EmojiProperties.Custom(DiscordConstants.PagesGoTo), PaginatorAction.Jump);
        }

        builder.WithSelectMenus(new List<StringMenuProperties> { selectMenuBuilder });

        return builder.Build();
    }

    public static StaticPaginator BuildSimpleStaticPaginator(this IList<PageBuilder> pages)
    {
        var builder = new StaticPaginatorBuilder()
            .WithPages(pages)
            .WithFooter(PaginatorFooter.None)
            .WithActionOnTimeout(ActionOnStop.DeleteInput);

        builder.WithOptions(new List<PaginatorButton>
        {
            new(EmojiProperties.Custom(DiscordConstants.PagesPrevious), PaginatorAction.Backward, ButtonStyle.Secondary),
            new(EmojiProperties.Custom(DiscordConstants.PagesNext), PaginatorAction.Forward, ButtonStyle.Secondary),
            new(EmojiProperties.Custom(DiscordConstants.PagesGoTo), PaginatorAction.Jump, ButtonStyle.Secondary)
        });

        return builder.Build();
    }
}

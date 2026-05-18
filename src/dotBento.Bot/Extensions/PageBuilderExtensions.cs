using dotBento.Bot.Resources;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using NetCord;
using NetCord.Rest;

namespace dotBento.Bot.Extensions;

public static class PageBuilderExtensions
{
    public static IComponentPaginator BuildStaticPaginator(this IList<PageBuilder> pages, string? customOptionId = null, EmojiProperties? optionEmote = null)
    {
        var builder = new ComponentPaginatorBuilder()
            .WithPageCount(pages.Count)
            .WithActionOnTimeout(ActionOnStop.DisableInput)
            .WithPageFactory(paginator =>
            {
                var page = pages[paginator.CurrentPageIndex];
                var navRow = new ActionRowProperties();

                if (pages.Count != 1 || customOptionId != null)
                {
                    navRow.AddPreviousButton(paginator, emote: EmojiProperties.Custom(DiscordConstants.PagesPrevious), style: ButtonStyle.Secondary);
                    navRow.AddNextButton(paginator, emote: EmojiProperties.Custom(DiscordConstants.PagesNext), style: ButtonStyle.Secondary);
                }

                if (customOptionId != null && optionEmote != null)
                {
                    navRow.AddButton(paginator, PaginatorAction.Jump, customOptionId, ButtonStyle.Primary, optionEmote);
                }
                else if (customOptionId == null && pages.Count >= 25)
                {
                    navRow.AddJumpButton(paginator, emote: EmojiProperties.Custom(DiscordConstants.PagesGoTo), style: ButtonStyle.Secondary);
                }

                return page.WithComponents([navRow]).Build();
            });

        return builder.Build();
    }

    public static IComponentPaginator BuildStaticPaginatorWithSelectMenu(this IList<PageBuilder> pages,
        StringMenuProperties selectMenuBuilder)
    {
        var builder = new ComponentPaginatorBuilder()
            .WithPageCount(pages.Count)
            .WithActionOnTimeout(ActionOnStop.DisableInput)
            .WithPageFactory(paginator =>
            {
                var page = pages[paginator.CurrentPageIndex];
                var navRow = new ActionRowProperties();

                if (pages.Count != 1)
                {
                    navRow.AddPreviousButton(paginator, emote: EmojiProperties.Custom(DiscordConstants.PagesPrevious), style: ButtonStyle.Secondary);
                    navRow.AddNextButton(paginator, emote: EmojiProperties.Custom(DiscordConstants.PagesNext), style: ButtonStyle.Secondary);
                }

                if (pages.Count >= 10)
                {
                    navRow.AddJumpButton(paginator, emote: EmojiProperties.Custom(DiscordConstants.PagesGoTo), style: ButtonStyle.Secondary);
                }

                return page.WithComponents([navRow, selectMenuBuilder]).Build();
            });

        return builder.Build();
    }

    public static IComponentPaginator BuildSimpleStaticPaginator(this IList<PageBuilder> pages)
    {
        var builder = new ComponentPaginatorBuilder()
            .WithPageCount(pages.Count)
            .WithActionOnTimeout(ActionOnStop.DisableInput)
            .WithPageFactory(paginator =>
            {
                var page = pages[paginator.CurrentPageIndex];
                var navRow = new ActionRowProperties();
                navRow.AddPreviousButton(paginator, emote: EmojiProperties.Custom(DiscordConstants.PagesPrevious), style: ButtonStyle.Secondary);
                navRow.AddNextButton(paginator, emote: EmojiProperties.Custom(DiscordConstants.PagesNext), style: ButtonStyle.Secondary);
                navRow.AddJumpButton(paginator, emote: EmojiProperties.Custom(DiscordConstants.PagesGoTo), style: ButtonStyle.Secondary);

                return page.WithComponents([navRow]).Build();
            });

        return builder.Build();
    }
}

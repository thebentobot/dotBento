using Discord;
using dotBento.Bot.Resources;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;

namespace dotBento.Bot.Extensions;

public static class PageBuilderExtensions
{
    public static StaticPaginator BuildStaticPaginator(this IList<PageBuilder> pages, string? customOptionId = null, IEmote? optionEmote = null)
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
            builder.AddOption(new KeyValuePair<IEmote, PaginatorAction>(Emote.Parse("<:pages_goto:1138849626234036264>"), PaginatorAction.Jump));
        }

        return builder.Build();
    }

    public static StaticPaginator BuildStaticPaginatorWithSelectMenu(this IList<PageBuilder> pages,
        SelectMenuBuilder selectMenuBuilder)
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
            builder.AddOption(new KeyValuePair<IEmote, PaginatorAction>(Emote.Parse("<:pages_goto:1138849626234036264>"), PaginatorAction.Jump));
        }
        
        builder.WithSelectMenus(new List<SelectMenuBuilder> { selectMenuBuilder });

        return builder.Build();
    }

    public static StaticPaginator BuildSimpleStaticPaginator(this IList<PageBuilder> pages)
    {
        var builder = new StaticPaginatorBuilder()
            .WithPages(pages)
            .WithFooter(PaginatorFooter.None)
            .WithActionOnTimeout(ActionOnStop.DeleteInput);

        builder.WithOptions(new Dictionary<IEmote, PaginatorAction>
        {
            { Emote.Parse("<:pages_previous:883825508507336704>"), PaginatorAction.Backward},
            { Emote.Parse("<:pages_next:883825508087922739>"), PaginatorAction.Forward},
            { Emote.Parse("<:pages_goto:1138849626234036264>"), PaginatorAction.Jump }
        });

        return builder.Build();
    }
}
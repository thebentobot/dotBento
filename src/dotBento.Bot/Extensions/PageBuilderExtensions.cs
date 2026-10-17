using dotBento.Bot.Services;
using Discord;
using dotBento.Bot.Resources;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;

namespace dotBento.Bot.Extensions;

public static class PageBuilderExtensions
{
    public static StaticPaginator BuildStaticPaginator(this IList<PageBuilder> pages, string? customOptionId = null, IEmote? optionEmote = null, BotPresentation? presentation = null)
    {
        presentation ??= new();
        var builder = new StaticPaginatorBuilder()
            .WithPages(pages)
            .WithFooter(PaginatorFooter.None)
            .WithActionOnTimeout(ActionOnStop.DeleteInput);

        if (pages.Count != 1 || customOptionId != null)
        {
            builder.WithOptions(presentation.PaginationEmotes);
        }

        if (customOptionId != null && optionEmote != null)
        {
            builder.AddOption(customOptionId, optionEmote, null, ButtonStyle.Primary);
        }

        if (customOptionId == null && pages.Count >= 25)
        {
            builder.AddOption(new KeyValuePair<IEmote, PaginatorAction>(presentation.Jump, PaginatorAction.Jump));
        }

        return builder.Build();
    }

    public static StaticPaginator BuildStaticPaginatorWithSelectMenu(this IList<PageBuilder> pages,
        SelectMenuBuilder selectMenuBuilder, BotPresentation? presentation = null)
    {
        presentation ??= new();
        var builder = new StaticPaginatorBuilder()
            .WithPages(pages)
            .WithFooter(PaginatorFooter.None)
            .WithActionOnTimeout(ActionOnStop.DeleteInput);

        if (pages.Count != 1)
        {
            builder.WithOptions(presentation.PaginationEmotes);
        }

        if (pages.Count >= 10)
        {
            builder.AddOption(new KeyValuePair<IEmote, PaginatorAction>(presentation.Jump, PaginatorAction.Jump));
        }
        
        builder.WithSelectMenus(new List<SelectMenuBuilder> { selectMenuBuilder });

        return builder.Build();
    }

    public static StaticPaginator BuildSimpleStaticPaginator(this IList<PageBuilder> pages, BotPresentation? presentation = null)
    {
        presentation ??= new();
        var builder = new StaticPaginatorBuilder()
            .WithPages(pages)
            .WithFooter(PaginatorFooter.None)
            .WithActionOnTimeout(ActionOnStop.DeleteInput);

        builder.WithOptions(new Dictionary<IEmote, PaginatorAction>
        {
            { presentation.PaginationEmotes.Single(x => x.Value == PaginatorAction.Backward).Key, PaginatorAction.Backward},
            { presentation.PaginationEmotes.Single(x => x.Value == PaginatorAction.Forward).Key, PaginatorAction.Forward},
            { presentation.Jump, PaginatorAction.Jump }
        });

        return builder.Build();
    }
}
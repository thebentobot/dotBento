using Discord;
using dotBento.Bot.Enums;
using dotBento.Domain.Enums;
using Fergun.Interactive.Pagination;

namespace dotBento.Bot.Models.Discord;

public class ResponseModel
{
    public EmbedAuthorBuilder EmbedAuthor { get; set; } = new();
    public EmbedBuilder Embed { get; set; } = new();
    public EmbedFooterBuilder EmbedFooter { get; set; } = new();
    public ComponentBuilder Components { get; set; }
    public ResponseType ResponseType { get; set; }
    public Stream Stream { get; set; }
    public string FileName { get; set; }
    public bool Spoiler { get; set; } = false;
    public string Text { get; set; }
    public StaticPaginator StaticPaginator { get; set; }
    public CommandResponse CommandResponse { get; set; } = CommandResponse.Ok;
}
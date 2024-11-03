using Discord;
using dotBento.Bot.Enums;
using dotBento.Domain.Enums;
using Fergun.Interactive.Pagination;

namespace dotBento.Bot.Models.Discord;

/// <summary>
/// The ResponseModel class. This is the standard way to return a response to a user.
/// </summary>
/// <remarks>
/// When using the ResponseModel class, take note of the following:
/// - For a <see cref="ResponseType.Paginator"/>, the <see cref="StaticPaginator"/> property must have a value.
/// - For a <see cref="ResponseType.Text"/>, the <see cref="Text"/> property must not be null or empty.
/// - For <see cref="ResponseType.ImageWithEmbed"/> or <see cref="ResponseType.ImageOnly"/>, the <see cref="Stream"/> and <see cref="FileName"/> properties must have a value.
/// </remarks>
public sealed class ResponseModel
{
    public ResponseModel(ResponseType responseType, string? text = null, StaticPaginator? staticPaginator = null, Stream? stream = null, string? fileName = null)
    {
        ResponseType = responseType;

        switch (ResponseType)
        {
            case ResponseType.Paginator when staticPaginator == null:
                throw new ArgumentException("StaticPaginator must not be null when ResponseType is Paginator");
            case ResponseType.Text when string.IsNullOrEmpty(text):
                throw new ArgumentException("Text must not be null or empty when ResponseType is Text");
            case ResponseType.ImageWithEmbed or ResponseType.ImageOnly 
            when (stream == null || string.IsNullOrEmpty(fileName)):
                throw new ArgumentException("Stream and FileName must not be null when ResponseType is ImageWithEmbed or ImageOnly");
        }

        StaticPaginator = staticPaginator;
        Text = text;
        Stream = stream;
        FileName = fileName;
    }

    public ResponseModel()
    {
        
    }

    public EmbedAuthorBuilder EmbedAuthor { get; set; } = new();
    public EmbedBuilder Embed { get; set; } = new();
    public EmbedFooterBuilder EmbedFooter { get; set; } = new();
    public ComponentBuilder? Components { get; set; }
    
    /// <summary>
    /// Gets or sets the response type.
    /// </summary>
    /// <remarks>
    /// If this property is set to <see cref="ResponseType.Paginator"/>, ensure to set the <see cref="StaticPaginator"/> property.
    /// If this property is set to <see cref="ResponseType.Text"/>, ensure to set the <see cref="Text"/> property.
    /// If this property is set to <see cref="ResponseType.ImageWithEmbed"/> or <see cref="ResponseType.ImageOnly"/>, ensure to set the <see cref="Stream"/> and <see cref="FileName"/> properties.
    /// </remarks>
    public ResponseType ResponseType { get; set; }
    
    /// <summary>
    /// Gets or sets the Stream property.
    /// </summary>
    /// <remarks>
    /// For <see cref="ResponseType.ImageWithEmbed"/> or <see cref="ResponseType.ImageOnly"/>, this property should not be null.
    /// </remarks>
    public Stream? Stream { get; set; }
    
    /// <summary>
    /// Gets or sets the FileName property.
    /// </summary>
    /// <remarks>
    /// For <see cref="ResponseType.ImageWithEmbed"/> or <see cref="ResponseType.ImageOnly"/>, this property should not be null.
    /// </remarks>
    public string? FileName { get; set; }
    public bool Spoiler { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the Text property.
    /// </summary>
    /// <remarks>
    /// For <see cref="ResponseType.Text"/>, this property must not be null or empty.
    /// </remarks>
    public string? Text { get; set; }
    
    /// <summary>
    /// Gets or sets the StaticPaginator property.
    /// </summary>
    /// <remarks>
    /// For <see cref="ResponseType.Paginator"/>, this property should not be null.
    /// </remarks>
    public StaticPaginator? StaticPaginator { get; set; }
    public CommandResponse CommandResponse { get; set; } = CommandResponse.Ok;
}
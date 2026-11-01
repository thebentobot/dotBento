namespace dotBento.Infrastructure.Services.Assets;

/// <summary>An image fetched from a public URL, ready to be inlined.</summary>
public sealed record FetchedAsset(byte[] Bytes, string ContentType)
{
    public string ToDataUri() => $"data:{ContentType};base64,{Convert.ToBase64String(Bytes)}";
}

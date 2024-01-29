using System.Text.Json.Serialization;

namespace dotBento.Infrastructure.Models.LastFm.UserInfo;

public sealed record UserInfoRegistered(
    string UnixTime,
    [property: JsonPropertyName("#text")] int Text
);
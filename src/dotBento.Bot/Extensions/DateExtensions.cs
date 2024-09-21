using CSharpFunctionalExtensions;

namespace dotBento.Bot.Extensions;

public static class DateExtensions
{
    public static Maybe<DateTimeOffset> ParseDateTimeOffset(this string date)
    {
        return DateTimeOffset.TryParse(date, out var result) ? result : Maybe<DateTimeOffset>.None;
    }
}
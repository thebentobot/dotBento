namespace dotBento.Domain.Entities;

public sealed record Reminder(int Id, long UserId, string Content, DateTimeOffset Date);
using dotBento.Domain.Entities;

namespace dotBento.Infrastructure.Extensions;

public static class ReminderExtensions
{
    public static Reminder Map(this EntityFramework.Entities.Reminder reminder) =>
        new(
            reminder.Id,
            reminder.UserId,
            reminder.Reminder1,
            reminder.Date);
}
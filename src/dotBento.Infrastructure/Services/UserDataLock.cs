using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace dotBento.Infrastructure.Services;

// Bounded local locks also protect cache writes; PostgreSQL locks coordinate separate processes.
internal static class UserDataLock
{
    private static readonly SemaphoreSlim[] Locks = Enumerable.Range(0, 256)
        .Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    public static async Task<T> ExecuteAsync<T>(
        IDbContextFactory<BotDbContext> contextFactory,
        long userId,
        Func<BotDbContext, Task<T>> operation,
        Func<T, Task> updateCache)
    {
        var gate = Locks[(int)((ulong)userId % (ulong)Locks.Length)];
        await gate.WaitAsync();
        try
        {
            await using var strategyDb = await contextFactory.CreateDbContextAsync();
            var strategy = strategyDb.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(async () =>
            {
                // Failed attempts must not reuse tracked entities or a failed transaction.
                await using var db = await contextFactory.CreateDbContextAsync();
                await using var transaction = db.Database.IsNpgsql()
                    ? await db.Database.BeginTransactionAsync()
                    : null;
                if (transaction is not null)
                    await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({userId})");

                var value = await operation(db);
                if (transaction is not null)
                    await transaction.CommitAsync();
                return value;
            });

            // Cache work must run only after a successful commit, outside database retries.
            await updateCache(result);
            return result;
        }
        finally
        {
            gate.Release();
        }
    }
}

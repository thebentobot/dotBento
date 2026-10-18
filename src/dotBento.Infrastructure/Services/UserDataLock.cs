using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace dotBento.Infrastructure.Services;

// Bounded local locks also protect cache writes; PostgreSQL locks coordinate separate processes.
internal sealed class UserDataLock : IAsyncDisposable
{
    private static readonly SemaphoreSlim[] Locks = Enumerable.Range(0, 256)
        .Select(_ => new SemaphoreSlim(1, 1)).ToArray();
    private readonly SemaphoreSlim gate;
    private readonly IDbContextTransaction? transaction;

    private UserDataLock(SemaphoreSlim gate, IDbContextTransaction? transaction)
    {
        this.gate = gate;
        this.transaction = transaction;
    }

    public static async Task<UserDataLock> AcquireAsync(BotDbContext db, long userId)
    {
        var gate = Locks[(int)((ulong)userId % (ulong)Locks.Length)];
        await gate.WaitAsync();
        IDbContextTransaction? transaction = null;
        try
        {
            if (db.Database.IsNpgsql())
            {
                transaction = await db.Database.BeginTransactionAsync();
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({userId})");
            }
            return new UserDataLock(gate, transaction);
        }
        catch
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
            gate.Release();
            throw;
        }
    }

    public async Task CommitAsync()
    {
        if (transaction is not null)
            await transaction.CommitAsync();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
        finally
        {
            gate.Release();
        }
    }
}

using System.Data.Common;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Gavel.Messaging.Outbox;

public sealed class OutboxSignalInterceptor(OutboxSignal signal) : SaveChangesInterceptor, IDbTransactionInterceptor
{
    private static readonly object Pending = new();

    private readonly ConditionalWeakTable<DbContext, object> _contextsWithNewMessages = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        TrackNewMessages(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        TrackNewMessages(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        NotifyUnlessInTransaction(eventData.Context);
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        NotifyUnlessInTransaction(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => Forget(eventData.Context);

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Forget(eventData.Context);
        return Task.CompletedTask;
    }

    public void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData) => NotifyIfPending(eventData.Context);

    public Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        NotifyIfPending(eventData.Context);
        return Task.CompletedTask;
    }

    public void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData) => Forget(eventData.Context);

    public Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Forget(eventData.Context);
        return Task.CompletedTask;
    }

    private void TrackNewMessages(DbContext? context)
    {
        if (context is not null && context.ChangeTracker.Entries<OutboxMessage>().Any(entry => entry.State == EntityState.Added))
        {
            _contextsWithNewMessages.AddOrUpdate(context, Pending);
        }
    }

    private void NotifyUnlessInTransaction(DbContext? context)
    {
        if (context?.Database.CurrentTransaction is null)
        {
            NotifyIfPending(context);
        }
    }

    private void NotifyIfPending(DbContext? context)
    {
        if (context is not null && _contextsWithNewMessages.Remove(context))
        {
            signal.Notify();
        }
    }

    private void Forget(DbContext? context)
    {
        if (context is not null)
        {
            _contextsWithNewMessages.Remove(context);
        }
    }
}

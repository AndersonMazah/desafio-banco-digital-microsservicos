using System.Data;
using System.Data.Common;

namespace ContaCorrente.Tests.Common.Fixtures;

public sealed class FakeDbTransaction : DbTransaction
{
    private readonly DbConnection _connection;

    public FakeDbTransaction(DbConnection connection, IsolationLevel isolationLevel)
    {
        _connection = connection;
        IsolationLevel = isolationLevel;
    }

    public bool WasCommitted { get; private set; }
    public bool WasRolledBack { get; private set; }

    public override IsolationLevel IsolationLevel { get; }
    protected override DbConnection DbConnection => _connection;

    public override void Commit()
    {
        WasCommitted = true;
    }

    public override void Rollback()
    {
        WasRolledBack = true;
    }

    public override Task CommitAsync(CancellationToken cancellationToken = default)
    {
        WasCommitted = true;
        return Task.CompletedTask;
    }

    public override Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        WasRolledBack = true;
        return Task.CompletedTask;
    }

}

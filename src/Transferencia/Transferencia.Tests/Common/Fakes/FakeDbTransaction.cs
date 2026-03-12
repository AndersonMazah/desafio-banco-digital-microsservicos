using System.Data;
using System.Data.Common;

namespace Transferencia.Tests.Common.Fakes;

internal sealed class FakeDbTransaction : DbTransaction
{
    private readonly DbConnection _connection;

    public FakeDbTransaction(DbConnection connection, IsolationLevel isolationLevel)
    {
        _connection = connection;
        IsolationLevel = isolationLevel;
    }

    public bool CommitCalled { get; private set; }

    public override IsolationLevel IsolationLevel { get; }
    protected override DbConnection DbConnection => _connection;

    public override void Commit()
    {
        CommitCalled = true;
    }

    public override void Rollback()
    {
    }

}

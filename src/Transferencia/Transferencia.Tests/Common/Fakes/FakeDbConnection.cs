using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Transferencia.Tests.Common.Fakes;

internal sealed class FakeDbConnection : DbConnection
{
    private ConnectionState _state = ConnectionState.Open;
    private string _connectionString = "Host=fake;";

    public FakeDbTransaction? LastTransaction { get; private set; }

    [AllowNull]
    public override string ConnectionString
    {
        get => _connectionString;
        set => _connectionString = value ?? string.Empty;
    }
    public override string Database => "fake";
    public override string DataSource => "fake";
    public override string ServerVersion => "1.0";
    public override ConnectionState State => _state;

    public override void ChangeDatabase(string databaseName)
    {
    }

    public override void Close()
    {
        _state = ConnectionState.Closed;
    }

    public override void Open()
    {
        _state = ConnectionState.Open;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        LastTransaction = new FakeDbTransaction(this, isolationLevel);
        return LastTransaction;
    }

    protected override DbCommand CreateDbCommand()
    {
        throw new NotSupportedException();
    }
}

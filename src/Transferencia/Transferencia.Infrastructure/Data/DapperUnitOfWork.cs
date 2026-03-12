using System.Data.Common;
using Transferencia.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Transferencia.Infrastructure.Data;

public sealed class DapperUnitOfWork : IUnitOfWork
{
    private readonly string _connectionString;

    public DapperUnitOfWork(IOptions<PostgresOptions> options)
    {
        _connectionString = options.Value.DefaultConnection;
    }

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

}

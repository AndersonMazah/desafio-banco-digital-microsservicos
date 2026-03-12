using System.Data.Common;
using ContaCorrente.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ContaCorrente.Infrastructure.Data;

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

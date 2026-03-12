using System.Data.Common;
using ContaCorrente.Domain.Interfaces;
using ContaCorrente.Infrastructure.Dpo;
using Dapper;
using ContaCorrenteEntity = ContaCorrente.Domain.Entities.ContaCorrente;

namespace ContaCorrente.Infrastructure.Repositories;

public sealed class ContaCorrenteRepository : IContaCorrenteRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public ContaCorrenteRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> CpfExisteAsync(string cpf, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM contacorrente WHERE cpf = @Cpf)";
        await using var connection = await _unitOfWork.CreateOpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { Cpf = cpf }, cancellationToken: cancellationToken));
    }

    public async Task<long> CriarAsync(ContaCorrenteEntity conta, CancellationToken cancellationToken)
    {
        const string sql = @"
INSERT INTO contacorrente (idcontacorrente, nome, cpf, senha_hash, salt)
VALUES (@IdContaCorrente, @Nome, @Cpf, @SenhaHash, @Salt)
RETURNING numero;";
        await using var connection = await _unitOfWork.CreateOpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(sql, new
        {
            conta.IdContaCorrente,
            conta.Nome,
            conta.Cpf,
            conta.SenhaHash,
            conta.Salt
        }, cancellationToken: cancellationToken));
    }

    public async Task<ContaCorrenteEntity?> ObterPorIdAsync(Guid idContaCorrente, CancellationToken cancellationToken, DbConnection? connection = null, DbTransaction? transaction = null)
    {
        const string sql = @"
SELECT idcontacorrente AS IdContaCorrente, numero AS Numero, cpf AS Cpf, nome AS Nome, ativo AS Ativo, senha_hash AS SenhaHash, salt AS Salt
FROM contacorrente
WHERE idcontacorrente = @IdContaCorrente";
        if (connection is not null)
        {
            var dpoShared = await connection.QuerySingleOrDefaultAsync<ContaCorrenteDpo>(new CommandDefinition(sql, new { IdContaCorrente = idContaCorrente }, transaction, cancellationToken: cancellationToken));
            return dpoShared is null ? null : Map(dpoShared);
        }
        await using var localConnection = await _unitOfWork.CreateOpenConnectionAsync(cancellationToken);
        var dpo = await localConnection.QuerySingleOrDefaultAsync<ContaCorrenteDpo>(new CommandDefinition(sql, new { IdContaCorrente = idContaCorrente }, cancellationToken: cancellationToken));
        return dpo is null ? null : Map(dpo);
    }

    public async Task<ContaCorrenteEntity?> ObterPorContaOuCpfAsync(long? conta, string? cpf, CancellationToken cancellationToken)
    {
        const string sqlConta = @"
SELECT idcontacorrente AS IdContaCorrente, numero AS Numero, cpf AS Cpf, nome AS Nome, ativo AS Ativo, senha_hash AS SenhaHash, salt AS Salt
FROM contacorrente
WHERE numero = @Conta";
        const string sqlCpf = @"
SELECT idcontacorrente AS IdContaCorrente, numero AS Numero, cpf AS Cpf, nome AS Nome, ativo AS Ativo, senha_hash AS SenhaHash, salt AS Salt
FROM contacorrente
WHERE cpf = @Cpf";
        await using var connection = await _unitOfWork.CreateOpenConnectionAsync(cancellationToken);
        ContaCorrenteDpo? dpo;
        if (conta.HasValue)
        {
            dpo = await connection.QuerySingleOrDefaultAsync<ContaCorrenteDpo>(new CommandDefinition(sqlConta, new { Conta = conta.Value }, cancellationToken: cancellationToken));
        }
        else
        {
            dpo = await connection.QuerySingleOrDefaultAsync<ContaCorrenteDpo>(new CommandDefinition(sqlCpf, new { Cpf = cpf }, cancellationToken: cancellationToken));
        }
        return dpo is null ? null : Map(dpo);
    }

    public async Task<ContaCorrenteEntity?> ObterPorCpfAsync(string cpf, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT idcontacorrente AS IdContaCorrente, numero AS Numero, cpf AS Cpf, nome AS Nome, ativo AS Ativo, senha_hash AS SenhaHash, salt AS Salt
FROM contacorrente
WHERE cpf = @Cpf";
        await using var connection = await _unitOfWork.CreateOpenConnectionAsync(cancellationToken);
        var dpo = await connection.QuerySingleOrDefaultAsync<ContaCorrenteDpo>(new CommandDefinition(sql, new { Cpf = cpf }, cancellationToken: cancellationToken));
        return dpo is null ? null : Map(dpo);
    }

    public async Task InativarAsync(Guid idContaCorrente, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE contacorrente SET ativo = FALSE WHERE idcontacorrente = @IdContaCorrente";
        await using var connection = await _unitOfWork.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { IdContaCorrente = idContaCorrente }, cancellationToken: cancellationToken));
    }

    private static ContaCorrenteEntity Map(ContaCorrenteDpo dpo)
    {
        return new()
        {
            IdContaCorrente = dpo.IdContaCorrente,
            Numero = dpo.Numero,
            Cpf = dpo.Cpf,
            Nome = dpo.Nome,
            Ativo = dpo.Ativo,
            SenhaHash = dpo.SenhaHash,
            Salt = dpo.Salt
        };
    }

}

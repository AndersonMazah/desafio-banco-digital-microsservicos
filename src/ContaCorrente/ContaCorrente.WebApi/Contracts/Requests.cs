using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;

namespace ContaCorrente.WebApi.Contracts;

public sealed class CadastrarUsuarioRequest
{
    [SwaggerSchema(Description = "Nome completo do cliente", Nullable = false)]
    public string? Nome { get; set; }

    [SwaggerSchema(Description = "CPF (com ou sem máscara)", Nullable = false, Format = "cpf")]
    public string? Cpf { get; set; }

    [SwaggerSchema(Description = "Senha de 6 caracteres numéricos", Nullable = false)]
    public string? Senha { get; set; }
}

public sealed class InativarUsuarioRequest
{
    [SwaggerSchema(Description = "Senha de 6 caracteres numéricos", Nullable = false)]
    public string? Senha { get; set; }
}

public sealed class LoginRequest
{
    [SwaggerSchema(Description = "Número da conta (opcional)")]
    public string? Conta { get; set; }

    [SwaggerSchema(Description = "CPF com ou sem máscara (opcional)")]
    public string? Cpf { get; set; }

    [SwaggerSchema(Description = "Senha de 6 caracteres numéricos", Nullable = false)]
    public string? Senha { get; set; }
}

public sealed class MovimentarContaRequest
{
    [SwaggerSchema(Description = "UUID da conta corrente de destino/origem", Nullable = false)]
    public Guid Conta { get; set; }

    [SwaggerSchema(Description = "UUID idempotente da requisição", Nullable = false)]
    [JsonPropertyName("id_requisicao")]
    public Guid IdRequisicao { get; set; }

    [SwaggerSchema(Description = "Valor da movimentação, maior que zero", Nullable = false)]
    public decimal Valor { get; set; }

    [SwaggerSchema(Description = "Tipo da movimentação: C (crédito) ou D (débito)", Nullable = false)]
    public string? Tipo { get; set; }
}

public sealed class ConsultarContaRequest
{
    [SwaggerSchema(Description = "CPF com ou sem máscara", Nullable = false)]
    public string? Cpf { get; set; }
}

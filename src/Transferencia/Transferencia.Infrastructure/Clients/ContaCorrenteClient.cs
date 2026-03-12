using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Transferencia.Domain.Interfaces;
using Transferencia.Domain.Models;
using Transferencia.Infrastructure.Data;

namespace Transferencia.Infrastructure.Clients;

public sealed class ContaCorrenteClient : IContaCorrenteClient
{
    private readonly HttpClient _httpClient;
    private readonly JwtOptions _jwtOptions;

    public ContaCorrenteClient(HttpClient httpClient, IOptions<JwtOptions> jwtOptions)
    {
        _httpClient = httpClient;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<ContaCorrenteOperationResult> MovimentarAsync(Guid conta, decimal valor, string tipo, CancellationToken cancellationToken)
    {
        var token = GenerateInternalToken();
        using var request = new HttpRequestMessage(HttpMethod.Post, "conta/movimentar")
        {
            Content = JsonContent.Create(new MovimentarContaCorrenteRequest
            {
                Conta = conta,
                IdRequisicao = Guid.NewGuid(),
                Valor = valor,
                Tipo = tipo
            })
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new ContaCorrenteOperationResult
            {
                StatusCode = 409,
                Message = "API ContaCorrente indisponível",
                Type = "TYPE_INTEGRATION_ERROR"
            };
        }
        using (response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return new ContaCorrenteOperationResult { StatusCode = 204 };
            }
            ContaCorrenteEnvelope? envelope = null;
            try
            {
                envelope = await response.Content.ReadFromJsonAsync<ContaCorrenteEnvelope>(cancellationToken: cancellationToken);
            }
            catch
            {
            }
            if (envelope is null)
            {
                return new ContaCorrenteOperationResult
                {
                    StatusCode = (int)response.StatusCode,
                    Message = "Falha ao processar integração com ContaCorrente",
                    Type = "TYPE_INTEGRATION_ERROR"
                };
            }
            return new ContaCorrenteOperationResult
            {
                StatusCode = (int)response.StatusCode,
                Message = envelope.Message,
                Type = envelope.Type,
                Data = envelope.Data
            };
        }
    }

    private string GenerateInternalToken()
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key)),
            SecurityAlgorithms.HmacSha256);

        var actorId = Guid.NewGuid().ToString();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, actorId),
            new Claim(JwtRegisteredClaimNames.Sub, actorId)
        };
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class MovimentarContaCorrenteRequest
    {
        public Guid Conta { get; set; }
        [JsonPropertyName("id_requisicao")]
        public Guid IdRequisicao { get; set; }
        public decimal Valor { get; set; }
        public string Tipo { get; set; } = string.Empty;
    }

    private sealed class ContaCorrenteEnvelope
    {
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

}

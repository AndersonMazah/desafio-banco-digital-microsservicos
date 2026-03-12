using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Transferencia.Tests.WebApi.Fixtures;
using Xunit;

namespace Transferencia.Tests.WebApi.Controllers;

public sealed class TransferenciaControllerTests : IClassFixture<TransferenciaWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TransferenciaControllerTests(TransferenciaWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DeveRetornar401QuandoTokenEstiverAusente()
    {
        var response = await _client.PostAsJsonAsync("/transferencia/transferir", new
        {
            id_requisicao = Guid.NewGuid(),
            conta_destino = Guid.NewGuid(),
            valor = 10m
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<EnvelopeResponse>();
        body.Should().BeEquivalentTo(new EnvelopeResponse(
            "Usuário não autorizado",
            "TYPE_USER_UNAUTHORIZED",
            null));
    }

    [Fact]
    public async Task DeveRetornar401QuandoTokenEstiverInvalido()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/transferencia/transferir")
        {
            Content = JsonContent.Create(new
            {
                id_requisicao = Guid.NewGuid(),
                conta_destino = Guid.NewGuid(),
                valor = 10m
            })
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "token-invalido");
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<EnvelopeResponse>();
        body.Should().BeEquivalentTo(new EnvelopeResponse(
            "Usuário não autorizado",
            "TYPE_USER_UNAUTHORIZED",
            null));
    }

    private sealed record EnvelopeResponse(string Message, string Type, object? Data);

}

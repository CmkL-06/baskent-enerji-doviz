using System.Net;
using Xunit;

namespace MoneyTransferTurkey.API.Tests;

/// <summary>
/// AllowAnonymous GET endpoint'lerinin erişilebilir olduğunu doğrular.
/// </summary>
public class ThemeControllerIntegrationTests : IClassFixture<MoneyTransferTurkeyWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ThemeControllerIntegrationTests(MoneyTransferTurkeyWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetThemes_Anonymous_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/Theme");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
        // Boş array veya tema listesi döner
        Assert.True(content.StartsWith("[") || content == "[]", "JSON array bekleniyor.");
    }
}

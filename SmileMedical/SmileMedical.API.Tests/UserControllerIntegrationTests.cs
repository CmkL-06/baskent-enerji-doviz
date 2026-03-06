using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SmileMedical.Entity.Modals.RequestModals.User;
using SmileMedical.Entity.Modals.ResponseModals.User;
using Xunit;

namespace SmileMedical.API.Tests;

public class UserControllerIntegrationTests : IClassFixture<SmileMedicalWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public UserControllerIntegrationTests(SmileMedicalWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndToken()
    {
        var request = new rm_user_login
        {
            Mail = "test@smilemedical.test",
            Password = "Test123!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/User/login", request);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<rsp_user_login>(content, JsonOptions);
        Assert.NotNull(loginResponse);
        Assert.NotNull(loginResponse.ApiToken);
        Assert.NotNull(loginResponse.UserInfo);
        Assert.Equal("test@smilemedical.test", loginResponse.UserInfo.Mail);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsNotFound()
    {
        var request = new rm_user_login
        {
            Mail = "wrong@test.com",
            Password = "WrongPassword"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/User/login", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsNotFound()
    {
        var request = new rm_user_login
        {
            Mail = "test@smilemedical.test",
            Password = "WrongPassword"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/User/login", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

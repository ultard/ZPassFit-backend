using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ZPassFit.Dto;

namespace ZPassFit.IntegrationTest.Infrastructure;

public static class ApiTestClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<string> LoginAsync(
        this HttpClient client,
        string email,
        string password
    )
    {
        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest(email, password)
        );
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        return body?.AccessToken
            ?? throw new InvalidOperationException("Login response did not include an access token.");
    }

    public static async Task<HttpResponseMessage> SendAuthenticatedAsync(
        this HttpClient client,
        string accessToken,
        HttpMethod method,
        string url,
        HttpContent? content = null
    )
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> GetAuthenticatedAsync(
        this HttpClient client,
        string accessToken,
        string url
    ) => client.SendAuthenticatedAsync(accessToken, HttpMethod.Get, url);

    public static Task<HttpResponseMessage> PostAuthenticatedJsonAsync<T>(
        this HttpClient client,
        string accessToken,
        string url,
        T body
    ) =>
        client.SendAuthenticatedAsync(
            accessToken,
            HttpMethod.Post,
            url,
            JsonContent.Create(body, options: JsonOptions)
        );
}

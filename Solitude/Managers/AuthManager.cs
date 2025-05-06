using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Solitude.Objects.Endpoints;

namespace Solitude.Managers;

public static class AuthManager
{
    private static DefaultEndpoint Endpoint { get; set; }

    static AuthManager()
    {
        Endpoint = new("https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token", RestSharp.Method.Post);

        Endpoint.WithHeaders(("Authorization", "basic M2Y2OWU1NmM3NjQ5NDkyYzhjYzI5ZjFhZjA4YThhMTI6YjUxZWU5Y2IxMjIzNGY1MGE2OWVmYTY3ZWY1MzgxMmU="));
        Endpoint.WithFormBody(("grant_type", "client_credentials"));
    }

    public static bool TryCreateToken([NotNullWhen(true)] out string? token)
    {
        token = string.Empty;

        var response = Endpoint.GetResponse();

        if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
        {
            Log.Error("Couldn't get token response. Status code {Code}", response.StatusCode);
            return false;
        }

        using var doc = JsonDocument.Parse(response.Content);

        if (!doc.RootElement.TryGetProperty("access_token", out var tokenProp))
            return false;

        token = tokenProp.GetString();

        if (string.IsNullOrEmpty(token))
            return false;

        return true;
    }
}

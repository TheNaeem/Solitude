using RestSharp;
using RestSharp.Authenticators;
using Solitude.Managers;

namespace Solitude.Objects.Auth;

public class EpicLauncherAuthenticator : IAuthenticator
{
    private string _token;

    public EpicLauncherAuthenticator()
    {
        if (!AuthManager.TryCreateToken(out var token))
        {
            throw new System.ArgumentNullException("Couldn't get token for launcher authenticator");
        }

        _token = token;
    }

    public ValueTask Authenticate(RestClient client, RestRequest request)
    {
        request.AddOrUpdateHeader("Authorization", $"Bearer {_token}");

        return new();
    }
}

using RestSharp;
using Solitude.Objects.Auth;

namespace Solitude.Objects.Endpoints;

public class EpicManifestEndpoint : DefaultEndpoint // took these endpoint models from Nightwatcher, another project of mine
{
    private static readonly RestClientOptions _options = new()
    {
        Authenticator = new EpicLauncherAuthenticator()
    };

    public EpicManifestEndpoint(string manifestPath)
        : base($"https://launcher-public-service-prod06.ol.epicgames.com/{manifestPath}", options: _options)
    {
    }
}

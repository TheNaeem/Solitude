using Solitude.Objects.Auth;

namespace Solitude.Objects.Endpoints;

public class EpicManifestEndpoint : DefaultEndpoint // took these endpoint models from Nightwatcher, another project of mine
{
    public EpicManifestEndpoint(string manifestPath)
        : base($"https://launcher-public-service-prod06.ol.epicgames.com/{manifestPath}")
    {
        Client.Authenticator = new EpicLauncherAuthenticator();
    }
}

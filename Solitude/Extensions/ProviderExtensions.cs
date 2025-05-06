using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports.Texture;

namespace Solitude.Extensions;

public static class ProviderExtensions
{
    public static void SaveTextureToDisk(this AbstractFileProvider provider, string texturePath, string outputDir)
    {
        if (!provider.TryLoadPackageObject<UTexture2D>(texturePath, out var texture))
            return;

        texture.SaveToDisk(outputDir);
    }
}

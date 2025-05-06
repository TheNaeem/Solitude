using System.Diagnostics;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;

namespace Solitude.Extensions;

public static class TextureExtensions
{
    public static void SaveToDisk(this UTexture2D texture, string outputDir)
    {
        try
        {
            var outputPath = Path.Join(outputDir, texture.Name + ".png");

            var sw = Stopwatch.StartNew();

            using var fileStream = File.OpenWrite(outputPath);
            var decoded = texture.Decode();
            fileStream.Write(decoded?.Encode(ETextureFormat.Png, out _));

            sw.Stop();

            Log.Information("Exported {Texture} in {Milliseconds} ms", texture.Name, sw.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            Log.Error("Couldn't export {Texture}", texture.Name);
            Log.Error(e, string.Empty);
        }
    }
}

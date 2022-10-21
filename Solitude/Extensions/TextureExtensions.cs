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

            using var decoded = texture.Decode();
            using var encoded = decoded?.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            using var fs = File.Create(outputPath);
            encoded?.AsStream().CopyTo(fs);

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

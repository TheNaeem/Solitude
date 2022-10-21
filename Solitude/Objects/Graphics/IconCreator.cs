using System;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;
using SkiaSharp;

namespace Solitude.Objects.Graphics;

public class IconCreator : IDisposable
{
    public SKSurface? Surface { get; protected set; }
    public SKCanvas? Canvas { get; protected set; }
    public SKImageInfo Info { get; set; }

    public IconCreator(int width, int height)
    {
        Info = new(width, height);

        Surface = SKSurface.Create(Info);
        Canvas = Surface.Canvas;
    }

    public void DrawTexture(UTexture2D texture, int x, int y, SKImageInfo? overrideInfo = null)
    {
        using var decoded = texture.Decode()?.Resize(overrideInfo ?? Info, SKFilterQuality.High);

        Canvas?.DrawBitmap(decoded, x, y);
    }

    public void DrawAndResizeImage(SKBitmap bmp, int x, int y, SKImageInfo resizeSize)
        => Canvas?.DrawBitmap(bmp.Resize(resizeSize, SKFilterQuality.High), x, y);

    public void DrawImage(SKBitmap bmp, int x, int y) => Canvas?.DrawBitmap(bmp, x, y);

    public SKImage? GetImage() => Surface?.Snapshot();

    public void Dispose()
    {
        Canvas?.Dispose();
        Surface?.Dispose();
    }
}

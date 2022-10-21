using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Solitude.Objects.Graphics;

public class MergedImageCreator : IDisposable
{
    public SKImageInfo ImagesSize { get; init; }
    private List<SKImage> Images { get; init; }

    public MergedImageCreator(SKImageInfo imagesSize, int reserveCount = 0)
    {
        ImagesSize = imagesSize;
        Images = new(reserveCount);
    }

    public void AddIcon(SKImage image) => Images.Add(image);

    public SKBitmap Build()
    {
        var imageOrder = (int)Math.Ceiling(Math.Sqrt(Images.Count));

        var combinedWidth = ImagesSize.Width * imageOrder;
        var combinedHeight = Images.Count / imageOrder;

        if (Images.Count % imageOrder != 0)
            combinedHeight++;

        combinedHeight *= ImagesSize.Height;

        var bitmap = new SKBitmap(combinedWidth, combinedHeight);
        using var canvas = new SKCanvas(bitmap);
        var point = new SKPoint(0, 0);

        for (int i = 0, placement = 0; i < Images.Count; i++)
        {
            if (placement >= imageOrder)
            {
                placement = 0;
                point.Y += ImagesSize.Height;
            }
            point.X = ImagesSize.Width * placement;

            canvas.DrawImage(Images[i], point);
            placement++;
        }

        return bitmap;
    }

    public void Dispose()
    {
        foreach (var image in Images)
            image.Dispose();
    }
}


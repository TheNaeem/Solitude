using SkiaSharp;

namespace Solitude.Objects.Graphics;

public class FortniteIconCreator : IconCreator
{
    private static SKBitmap IconBase = SKBitmap.Decode(Properties.Resources.FortniteIconBase);
    private static SKTypeface BurbankFont = SKTypeface.FromData(SKData.CreateCopy(Properties.Resources.BurbankFont));

    public FortniteIconCreator(SKImageInfo info) : base(info.Width, info.Height)
    {
    }

    public FortniteIconCreator() : base(1024, 1124)
    {
    }

    public void DrawRarityBackground(string name, SKImageInfo? overrideInfo = null)
    {
        if (name.Contains("::"))
            name = name.Split("::")[1];

        var bg = Properties.Resources.ResourceManager.GetObject(name) as byte[];

        if (bg is null)
        {
            DrawRarityBackground("Unattainable");
            return;
        }

        Canvas?.DrawBitmap(SKBitmap.Decode(bg).Resize(overrideInfo ?? Info, SKFilterQuality.High), 0, 0);
    }

    public void DrawDisplayName(string name)
    {
        using var textPaint = new SKPaint()
        {
            TextSize = 32,
            IsAntialias = true,
            Color = SKColors.GhostWhite,
            Typeface = BurbankFont,
            TextAlign = SKTextAlign.Center,
            Style = SKPaintStyle.Fill,
            FakeBoldText = true
        };

        while (textPaint.MeasureText(name) > 475)
        {
            textPaint.TextSize *= 0.9f;
        }

        Canvas?.DrawBitmap(IconBase, 0, 0);

        Canvas?.DrawText(name, Info.Width / 2, Info.Height - 20, textPaint);
    }
}


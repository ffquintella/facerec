using SkiaSharp;

namespace MLFaceLib.ImageTools;

public static class NormalizationHelper
{
    public static float[][,] NormalizeSKBitmap(SKBitmap bitmap)
    {
        var normalizedArray = new[]
        {
            new float[bitmap.Height, bitmap.Width],
            new float[bitmap.Height, bitmap.Width],
            new float[bitmap.Height, bitmap.Width]
        };

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                normalizedArray[0][y, x] = color.Red / 255.0f;
                normalizedArray[1][y, x] = color.Green / 255.0f;
                normalizedArray[2][y, x] = color.Blue / 255.0f;
            }
        }

        return normalizedArray;
    }
}
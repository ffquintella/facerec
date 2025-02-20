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
    
    public static SKBitmap SkMeanNormalization(SKBitmap image, float[] mean, float[] std)
    {
        if (mean.Length != 3 || std.Length != 3)
            throw new ArgumentException("Mean and Standard Deviation must have 3 values (RGB).");

        int width = image.Width;
        int height = image.Height;
        SKBitmap normalizedImage = new SKBitmap(width, height, SKColorType.Rgb888x, SKAlphaType.Opaque);

        // Iterate over each pixel and apply normalization
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SKColor pixel = image.GetPixel(x, y);

                float r = pixel.Red / 255.0f;
                float g = pixel.Green / 255.0f;
                float b = pixel.Blue / 255.0f;

                // Normalize using PyTorch formula
                float rNorm = (r - mean[0]) / std[0];
                float gNorm = (g - mean[1]) / std[1];
                float bNorm = (b - mean[2]) / std[2];

                // Convert back to [0, 255] and clamp values
                byte newR = (byte)Math.Clamp(rNorm * 255, 0, 255);
                byte newG = (byte)Math.Clamp(gNorm * 255, 0, 255);
                byte newB = (byte)Math.Clamp(bNorm * 255, 0, 255);

                normalizedImage.SetPixel(x, y, new SKColor(newR, newG, newB));
            }
        }

        return normalizedImage;
    }
    
    public static float[,,] MeanNormalization(SKBitmap image, float[] mean, float[] std)
    {
        if (mean.Length != 3 || std.Length != 3)
            throw new ArgumentException("Mean and Standard Deviation must have 3 values (RGB).");

        int width = image.Width;
        int height = image.Height;
        float[,,] normalizedTensor = new float[3, height, width];  // Shape: (C, H, W) like PyTorch

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SKColor pixel = image.GetPixel(x, y);

                float r = pixel.Red / 255.0f;
                float g = pixel.Green / 255.0f;
                float b = pixel.Blue / 255.0f;

                // Normalize using PyTorch formula
                normalizedTensor[0, y, x] = (r - mean[0]) / std[0];  // Red channel
                normalizedTensor[1, y, x] = (g - mean[1]) / std[1];  // Green channel
                normalizedTensor[2, y, x] = (b - mean[2]) / std[2];  // Blue channel
            }
        }

        return normalizedTensor;
    }
    
    public static float[][,] RGBMeanNormalization(SKBitmap image, float[] mean, float[] std)
    {
        if (mean.Length != 3 || std.Length != 3)
            throw new ArgumentException("Mean and Standard Deviation must have 3 values (RGB).");

        int width = image.Width;
        int height = image.Height;

        // Create float arrays for each color channel
        float[,] redChannel = new float[height, width];
        float[,] greenChannel = new float[height, width];
        float[,] blueChannel = new float[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SKColor pixel = image.GetPixel(x, y);

                // Normalize using PyTorch formula
                redChannel[y, x] = (pixel.Red / 255.0f - mean[0]) / std[0];
                greenChannel[y, x] = (pixel.Green / 255.0f - mean[1]) / std[1];
                blueChannel[y, x] = (pixel.Blue / 255.0f - mean[2]) / std[2];
            }
        }

        return new float[][,] { redChannel, greenChannel, blueChannel };  // Returns an array of matrices
    }
}
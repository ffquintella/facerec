using SkiaSharp;

namespace MLFaceLib.ImageTools;

public static class SkBitmapExtensions
{
    /// <summary>
    /// Converts an SKBitmap (RGB) to a float[3][] array where each channel (R, G, B) is stored separately.
    /// </summary>
    /// <param name="bitmap">The SKBitmap image.</param>
    /// <returns>A float[3][] array containing normalized RGB values (0 to 1).</returns>
    public static float[][] ToFloatArray(this SKBitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        int totalPixels = width * height;

        // Initialize float arrays for each RGB channel
        float[] redChannel = new float[totalPixels];
        float[] greenChannel = new float[totalPixels];
        float[] blueChannel = new float[totalPixels];

        int index = 0;

        // Loop through each pixel
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, y);

                // Normalize values from [0,255] to [0,1] (optional)
                redChannel[index] = pixel.Red / 255.0f;
                greenChannel[index] = pixel.Green / 255.0f;
                blueChannel[index] = pixel.Blue / 255.0f;

                index++;
            }
        }

        return new float[][] { redChannel, greenChannel, blueChannel };
    }
    
    /// <summary>
    /// Converts an SKBitmap to a float[][,] array and normalizes using mean and standard deviation.
    /// </summary>
    /// <param name="bitmap">The SKBitmap image.</param>
    /// <param name="mean">An array of 3 floats representing the mean for R, G, B.</param>
    /// <param name="std">An array of 3 floats representing the standard deviation for R, G, B.</param>
    /// <returns>A float[][,] array where each channel (R, G, B) is a 2D array [height, width].</returns>
    public static float[][,] ToNormalizedFloatArray(this SKBitmap bitmap, float[] mean, float[] std)
    {
        if (bitmap == null)
            throw new ArgumentNullException(nameof(bitmap));
        if (mean.Length != 3 || std.Length != 3)
            throw new ArgumentException("Mean and Standard Deviation must each have 3 values (R, G, B).");

        int width = bitmap.Width;
        int height = bitmap.Height;

        // Initialize 2D float arrays for each channel
        float[,] redChannel = new float[height, width];
        float[,] greenChannel = new float[height, width];
        float[,] blueChannel = new float[height, width];

        // Loop through each pixel in the image
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, y);

                // Convert to float in range [0,1]
                float r = pixel.Red / 255.0f;
                float g = pixel.Green / 255.0f;
                float b = pixel.Blue / 255.0f;

                // Apply normalization: (X - mean) / std
                redChannel[y, x] = (r - mean[0]) / std[0];
                greenChannel[y, x] = (g - mean[1]) / std[1];
                blueChannel[y, x] = (b - mean[2]) / std[2];
            }
        }

        return new float[][,] { redChannel, greenChannel, blueChannel };
    }
    
    /// <summary>
    /// Resizes an SKBitmap to the specified width and height.
    /// </summary>
    /// <param name="bitmap">The input SKBitmap.</param>
    /// <param name="newWidth">The desired width.</param>
    /// <param name="newHeight">The desired height.</param>
    /// <param name="quality">Resampling quality (Low, Medium, High, or Best).</param>
    /// <returns>A new resized SKBitmap.</returns>
    public static SKBitmap ResizeBitmap(this SKBitmap bitmap, int newWidth, int newHeight, SKFilterQuality quality = SKFilterQuality.High)
    {
        if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

        // Create a new empty bitmap with desired size
        SKBitmap resizedBitmap = new SKBitmap(newWidth, newHeight, bitmap.ColorType, bitmap.AlphaType);

        // Resize with the specified quality
        using (SKCanvas canvas = new SKCanvas(resizedBitmap))
        {
            SKRect destRect = new SKRect(0, 0, newWidth, newHeight);
            canvas.DrawBitmap(bitmap, destRect, new SKPaint { FilterQuality = quality });
        }

        return resizedBitmap;
    }
}

using FaceONNX;
using Microsoft.ML.OnnxRuntime;
using SkiaDrawing;
using SkiaSharp;
using UMapx.Imaging;

namespace MLFaceLib.ONNX;

public abstract class BaseClassifier: IFaceClassifier
{
    /// <summary>
    /// Inference session.
    /// </summary>
    protected  InferenceSession _session;
    
    /// <inheritdoc/>
    public float[] Forward(Bitmap image)
    {
        var rgb = image.ToRGB(false);
        return Forward(rgb);
    }

    public abstract float[] Forward(float[][,] image);
    
    
    /// <summary>
    /// Converts an SKBitmap to grayscale.
    /// </summary>
    private static SKBitmap ConvertToGrayscale(SKBitmap bitmap)
    {
        SKBitmap grayBitmap = new SKBitmap(bitmap.Width, bitmap.Height);

        using (SKCanvas canvas = new SKCanvas(grayBitmap))
        using (SKPaint paint = new SKPaint())
        {
            // Use a color matrix filter for grayscale conversion
            paint.ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                0.3f, 0.59f, 0.11f, 0, 0,  // Red
                0.3f, 0.59f, 0.11f, 0, 0,  // Green
                0.3f, 0.59f, 0.11f, 0, 0,  // Blue
                0,    0,     0,     1, 0   // Alpha
            });

            // Draw the original image onto the new canvas using the grayscale filter
            canvas.DrawBitmap(bitmap, 0, 0, paint);
        }

        return grayBitmap;
    }
    
    #region IDisposable

    private bool _disposed;

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _session?.Dispose();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Destructor.
    /// </summary>
    ~BaseClassifier()
    {
        Dispose(false);
    }

    #endregion
}
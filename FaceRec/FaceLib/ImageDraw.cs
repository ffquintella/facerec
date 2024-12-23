using System.Drawing;
using SkiaSharp;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace FaceLib;

public static class ImageDraw
{
    public static Bitmap DrawRectanglesOnFaces(byte[] imageBytes, Rectangle[] faces)
    {
        using (var ms = new MemoryStream(imageBytes))
        {
            // Load the original image
            using (var originalBitmap = SKBitmap.Decode(ms))
            {
                // Create a new bitmap with the same dimensions
                using (var newBitmap = new SKBitmap(originalBitmap.Width, originalBitmap.Height))
                {
                    using (var canvas = new SKCanvas(newBitmap))
                    {
                        // Draw the original image onto the canvas
                        canvas.DrawBitmap(originalBitmap, 0, 0);

                        // Set the paint for drawing rectangles
                        var paint = new SKPaint
                        {
                            Color = SKColors.Red,
                            Style = SKPaintStyle.Stroke,
                            StrokeWidth = 2
                        };

                        // Draw rectangles around the detected faces
                        foreach (var face in faces)
                        {
                            var rect = new SKRect(face.X, face.Y, face.X + face.Width, face.Y + face.Height);
                            canvas.DrawRect(rect, paint);
                        }
                    }

                    // Convert the new bitmap to a MemoryStream
                    using (var image = SKImage.FromBitmap(newBitmap))
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                    using (var outputStream = new MemoryStream())
                    {
                        data.SaveTo(outputStream);
                        outputStream.Seek(0, SeekOrigin.Begin);

                        // Return the new bitmap as an Avalonia Bitmap
                        return new Bitmap(outputStream);
                    }
                }
            }
        }
    }
}
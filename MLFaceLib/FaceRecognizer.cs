

using FaceONNX;
using SkiaDrawing;
using SkiaSharp;

namespace MLFaceLib;

public class FaceRecognizer
{
    
    static FaceDetector faceDetector;
    static Face68LandmarksExtractor _faceLandmarksExtractor;
    static FaceEmbedder _faceEmbedder;
    
    private Embeddings FaceEmbeddings { get; set; } = new Embeddings();
    
    
    public FaceRecognizer()
    {
        faceDetector = new FaceDetector();
        _faceLandmarksExtractor = new Face68LandmarksExtractor();
        _faceEmbedder = new FaceEmbedder();
    }
    
    public Task TrainAsync(string imagePath)
    {
        var fits = Directory.GetFiles(imagePath);
        
        foreach (var fit in fits)
        {
            using var theImage = SKBitmap.Decode(fit);
            var embedding = GetEmbedding(theImage);
            var name = Path.GetFileNameWithoutExtension(fit);
            FaceEmbeddings.Add(embedding, name);

        }
        
        return Task.CompletedTask;
    }
    
    
    static float[] GetEmbedding(SKBitmap image)
    {
        var array = GetImageFloatArray(image);
        var rectangles = faceDetector.Forward(array);
        var rectangle = rectangles.FirstOrDefault().Box;

        if (!rectangle.IsEmpty)
        {
            // landmarks
            var points = _faceLandmarksExtractor.Forward(array, rectangle);
            var angle = points.RotationAngle;

            // alignment
            var aligned = FaceProcessingExtensions.Align(array, rectangle, angle);
            return _faceEmbedder.Forward(aligned);
        }

        return new float[512];
    }
    
    static float[][,] GetImageFloatArray(SKBitmap image)
    {
        var array = new[]
        {
            new float[image.Height, image.Width],
            new float[image.Height, image.Width],
            new float[image.Height, image.Width]
        };

        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var color = image.GetPixel(x, y);
                array[2][y, x] = color.Red / 255.0F;
                array[1][y, x] = color.Green / 255.0F;
                array[0][y, x] = color.Blue / 255.0F;
            }
        }

        return array;
    }
}
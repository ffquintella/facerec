

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
    
    public void Clear()
    {
        FaceEmbeddings.Clear();
    }
    
    
    public async Task TrainAsync(string imageDirPath)
    {
        // Load the images
        var images = Directory.GetFiles(imageDirPath, "*.png");
        
        foreach (var fit in images)
        {
            using var theImage = SKBitmap.Decode(fit);
            var embedding = GetEmbedding(theImage);
            
            var fileName = Path.GetFileNameWithoutExtension(fit);
            
            var name = fileName.Split('(')[1].Split(')')[0].Trim();
            
            FaceEmbeddings.Add(embedding, name);

        }
    }
    
    public async Task SaveModel(string dataFilePath)
    {
        await FaceEmbeddings.SaveToFileAsync(dataFilePath);
    }

    public async Task<(string?,float)> Predict(SKBitmap image)
    {
        try
        {
            var embedding = GetEmbedding(image);
            var proto = FaceEmbeddings.FromSimilarity(embedding);
            var label = proto.Item1;
            var similarity = proto.Item2; 
            
            return (label, similarity);
            
        }catch(Exception e)
        {
            return (null, 0);
        }
        
    }
    
    public async Task Load(string dataFilePath)
    {
        if(File.Exists(dataFilePath)) await FaceEmbeddings.LoadFromFileAsync(dataFilePath);
        else throw new Exception("Data file not found.");
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
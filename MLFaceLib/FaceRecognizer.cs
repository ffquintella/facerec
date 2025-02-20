

using FaceONNX;
using Microsoft.ML.OnnxRuntime;
using MLFaceLib.ImageTools;
using MLFaceLib.ONNX;
using SkiaDrawing;
using SkiaSharp;
using UMapx.Core;

namespace MLFaceLib;

public class FaceRecognizer
{
    
    static FaceDetector faceDetector;
    static SpoofClassifier faceSpoofClassifier;
    static Face68LandmarksExtractor _faceLandmarksExtractor;
    static FaceEmbedder _faceEmbedder;
    
    private Embeddings FaceEmbeddings { get; set; } = new Embeddings();
    
    
    public FaceRecognizer()
    {
        
        faceSpoofClassifier = new SpoofClassifier();
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

    public async Task<(string?,float, bool, float)> Predict(SKBitmap image)
    {
        try
        {
            //var grayImage = ConvertToGrayscale(image);
            //var normalizedArray = GetImageFloatArray(image);
            
            //var grayImage = ConvertToGrayscale(image);
            //var normalizedArray = GetImageFloatArray(grayImage);
            

            var normalizedArray = ImageTools.NormalizationHelper.RGBMeanNormalization(image,
                [0.485f, 0.456f, 0.406f], [0.229f, 0.224f, 0.225f]);
            
            var spoof = faceSpoofClassifier.Forward(normalizedArray);
            
            var max = Matrice.Max(spoof, out int realPredict);
            
            bool isReal = false;

            if (max < 20)
            {
                var realLabel = SpoofClassifier.Labels[realPredict];
                isReal = realLabel == "Real";
            }
            
            
            var embedding = GetEmbedding(image);
            var proto = FaceEmbeddings.FromSimilarity(embedding);
            var label = proto.Item1;
            var similarity = proto.Item2; 
            
            if(similarity < 0.5) label = "Unknown";
            
            return (label, similarity, isReal, max);
            
        }catch(Exception e)
        {
            return (null, 0, false, 0);
        }
        
    }
    
    public async Task Load(string dataFilePath)
    {
        if(File.Exists(dataFilePath)) await FaceEmbeddings.LoadFromFileAsync(dataFilePath);
        else throw new Exception("Data file not found.");
    }

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
    

    static float[] GetEmbedding(SKBitmap image, bool detectFace = false)
    {
        var array = GetImageFloatArray(image);

        Rectangle rectangle = new Rectangle();
        
        if (detectFace){
            var rectangles = faceDetector.Forward(array);
            rectangle = rectangles.FirstOrDefault().Box;
        }
        else
        {
            rectangle = new Rectangle(0, 0, image.Width, image.Height);
        }

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


using FaceONNX;
using Microsoft.ML.OnnxRuntime;
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
            
            var spoof = faceSpoofClassifier.Forward(new Bitmap(image));
            var max = Matrice.Max(spoof, out int realPredict);
            var realLabel = FaceDepthClassifier.Labels[realPredict];
            bool isReal = realLabel == "Real";
            
            
            
            var embedding = GetEmbedding(image);
            var proto = FaceEmbeddings.FromSimilarity(embedding);
            var label = proto.Item1;
            var similarity = proto.Item2; 
            
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
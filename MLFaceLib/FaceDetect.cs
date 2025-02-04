
using MLFaceLib.HaarCascadeDetection;
using SixLabors.ImageSharp;

namespace MLFaceLib;

public static class FaceDetect
{
    //private static HaarCascade cascade = HaarCascade.LoadEmbeded("MLFaceLib.OpenCVXMLs.haarcascade_frontalface_default.xml");
    
    private static HaarCascadeClassifier cascade = HaarCascadeClassifier.LoadFromEmbeddedResource("MLFaceLib.OpenCVXMLs.haarcascade_frontalface_default.xml");
    
    public static Rectangle[]? DetectFace(Image image)
    {
        
        HaarCascadeDetection.HaarObjectDetector detector = new HaarCascadeDetection.HaarObjectDetector(cascade);
        
        // Run detection.
        List<Rectangle> faces = detector.DetectObjects(image, scaleFactor: 1.1);

        // Exibe os resultados
        Console.WriteLine($"Detected {faces.Count} faces:");
        foreach (var rect in faces)
        {
            Console.WriteLine($"Face at ({rect.X}, {rect.Y}) with size {rect.Width}x{rect.Height}");
        }
        
        return faces.ToArray();

    }
}
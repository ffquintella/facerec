using System.Drawing;
using System.Drawing.Imaging;


namespace MLFaceLib;

public static class FaceDetect
{
    public static Rectangle[]? DetectFace(Bitmap image)
    {

        // Cria o detector (usa os parâmetros fixos definidos no construtor)
        HaarObjectDetector detector = new HaarObjectDetector();

        // Realiza a detecção
        var detections = detector.DetectObjects(image);

        // Exibe os resultados
        Console.WriteLine("Objetos detectados: " + detections.Count);
        foreach (var rect in detections)
        {
            Console.WriteLine($"Posição: {rect.X}, {rect.Y}, Tamanho: {rect.Width}x{rect.Height}");
        }
        
        return detections.ToArray();

    }
}
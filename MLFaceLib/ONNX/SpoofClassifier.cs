using FaceONNX;
using FaceONNX.Properties;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaDrawing;
using UMapx.Core;
using UMapx.Imaging;

namespace MLFaceLib.ONNX;

public class SpoofClassifier: BaseClassifier
{
    #region Private data
    
        private string _modelFile = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "models", "antispoof-efficient-2.onnx");
        
    #endregion

    #region Constructor

    /// <summary>
    /// Initializes face depth classifier.
    /// </summary>
    public SpoofClassifier()
    {
        _session = new InferenceSession(_modelFile);
    }

    /// <summary>
    /// Initializes face depth classifier.
    /// </summary>
    /// <param name="options">Session options</param>
    public SpoofClassifier(SessionOptions options)
    {
        _session = new InferenceSession(_modelFile, options);
    }
    

    #endregion

    #region Properties

    /// <summary>
    /// Returns the labels.
    /// </summary>
    public static readonly string[] Labels = new string[] { "Fake", "Real" };

    #endregion

    #region Methods
    

    /// <inheritdoc/>
    public override float[] Forward(float[][,] image)
    {
        if (image.Length != 3)
            throw new ArgumentException("Image must be in RGB terms");

        //var size = new Size(960, 960);
        var size = new Size(900, 900);
        var resized = new float[3][,];

        for (int i = 0; i < image.Length; i++)
        {
            resized[i] = image[i].Resize(size.Height, size.Width, UMapx.Core.InterpolationMode.Bilinear);
        }

        var inputMeta = _session.InputMetadata;
        var name = inputMeta.Keys.ToArray()[0];

        // pre-processing 
        var dimentions = new int[] { 1, 3, size.Height, size.Width };
        var tensors = resized.ToFloatTensor(true);
        //tensors.Compute(127.0f, Matrice.Sub);
        //var inputData = tensors.Average();
        
        // Flatten the tensors array
        var flatTensors = tensors.SelectMany(t => t.Cast<float>()).ToArray();

        // session run
        var t = new DenseTensor<float>(flatTensors, dimentions);
        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(name, t) };
        using var outputs = _session.Run(inputs);
        var results = outputs.ToArray();
        var length = results.Length;
        var confidences = results[length - 1].AsTensor<float>().ToArray();

        return confidences;
    }

    #endregion

    
}
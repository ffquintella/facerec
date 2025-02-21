using FaceONNX;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using MLFaceLib.ImageTools;
using SkiaDrawing;
using SkiaSharp;
using UMapx.Core;
using UMapx.Imaging;

namespace MLFaceLib.ONNX;

public class ColorIdentifier: BaseClassifier
{
    #region Private data
    
    private string _modelFile = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "models", "chromatic-efficient-2.onnx");
        
    #endregion
    
    #region Constructor

    /// <summary>
    /// Initializes face depth classifier.
    /// </summary>
    public ColorIdentifier()
    {
        _session = new InferenceSession(_modelFile);
    }

    /// <summary>
    /// Initializes face depth classifier.
    /// </summary>
    /// <param name="options">Session options</param>
    public ColorIdentifier(SessionOptions options)
    {
        _session = new InferenceSession(_modelFile, options);
    }
    

    #endregion
    
    #region Properties

    /// <summary>
    /// Returns the labels.
    /// </summary>
    public static readonly string[] Labels = new string[] { "NC", "R", "W", "G", "B" };

    #endregion
    
    #region Methods


    public float[] Forward(SKBitmap image)
    {
        //var normalizedArray = NormalizationHelper.RGBMeanNormalization(extractedPiece,
        //    [0.485f, 0.456f, 0.406f], [0.229f, 0.224f, 0.225f]);
        
        var resized = image.ResizeBitmap(960, 960, SKFilterQuality.High);
        
        var array = resized.ToNormalizedFloatArray([0.485f, 0.456f, 0.406f], [0.229f, 0.224f, 0.225f]);
        
        return Forward(array);
    }
    
    public override float[] Forward(float[][,] image)
    {
        
        
        if (image.Length != 3)
            throw new ArgumentException("Image must be in RGB terms");

        var size = new Size(960, 960);
        /*
        var resized = new float[3][,];

        for (int i = 0; i < image.Length; i++)
        {
            resized[i] = image[i].Resize(size.Height, size.Width, UMapx.Core.InterpolationMode.Bilinear);
        }*/

        var inputMeta = _session.InputMetadata;
        var name = inputMeta.Keys.ToArray()[0];

        // pre-processing 
        var dimentions = new int[] { 1, 3, size.Height, size.Width };
        var tensors = image.ToFloatTensor(true);
        //tensors.Compute(127.0f, Matrice.Sub);

        
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
    ~ColorIdentifier()
    {
        Dispose(false);
    }

    #endregion
}
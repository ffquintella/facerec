using FaceONNX;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaDrawing;
using UMapx.Core;
using UMapx.Imaging;

namespace MLFaceLib.ONNX;

public class ColorIdentifier: IFaceClassifier
{
    #region Private data
    /// <summary>
    /// Inference session.
    /// </summary>
    private readonly InferenceSession _session;
    
    private string _modelFile = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "models", "chromatic-efficient-1_50.onnx");
        
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
    public static readonly string[] Labels = new string[] { "1", "2", "3", "4", "5" };

    #endregion
    
     #region Methods

    /// <inheritdoc/>
    public float[] Forward(Bitmap image)
    {
        var rgb = image.ToRGB(false);
        return Forward(rgb);
    }

    /// <inheritdoc/>
    public float[] Forward(float[][,] image)
    {
        if (image.Length != 3)
            throw new ArgumentException("Image must be in RGB terms");

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
        tensors.Compute(127.0f, Matrice.Sub);
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
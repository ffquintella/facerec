using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Platform;
using FaceONNX;
using FFmpeg.AutoGen;
using FlashCap;
using MLFaceLib;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using SeeShark;
using SeeShark.Decode;
using SeeShark.Device;
using SeeShark.FFmpeg;
using SkiaSharp;
using PixelFormat = SeeShark.PixelFormat;
using Rectangle = Avalonia.Controls.Shapes.Rectangle;

namespace demoapp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private int frameCount = 0;

    private string baseFolder = "/Users/felipe/tmp";
    
    public string VideoRecognition  { get; } = "Sample Video Recognition APP";
    
    public MainWindowViewModel()
    {
        Image = new Bitmap(AssetLoader.Open(new Uri("avares://demoapp/Assets/placeholder.png")));
        
        // FFMPEG setup
        FFmpegManager.SetupFFmpeg (["/opt/homebrew/Cellar/ffmpeg/7.1_4/lib/"]);
        
        MainWindowViewModel.ActiveWindow = this;

        _ = Init();

    }
    
    
    private bool _isCameraEnabled;
    
    public bool IsCameraEnabled
    {
        get => _isCameraEnabled;
        set => this.RaiseAndSetIfChanged(ref _isCameraEnabled, value);
    }
    
    private bool _isRecognitionEnabled;
    
    public bool IsRecognitionEnabled
    {
        get => _isRecognitionEnabled;
        set => this.RaiseAndSetIfChanged(ref _isRecognitionEnabled, value);
    }
    
    private Bitmap _image;
    
    public Bitmap Image
    {
        get => _image;
        set => this.RaiseAndSetIfChanged(ref _image, value);
    }
    
    private SKImage _skImage;
    
    public SKImage SkImage
    {
        get => _skImage;
        set => this.RaiseAndSetIfChanged(ref _skImage, value);
    }
    
    private string _personName;
    
    public string PersonName
    {
        get => _personName;
        set => this.RaiseAndSetIfChanged(ref _personName, value);
    }
    
    private string _identity;
    
    public string Identity
    {
        get => _identity;
        set => this.RaiseAndSetIfChanged(ref _identity, value);
    }

    private CaptureDevice? _device;
    
    private CameraManager _cameraManager;
    
    private VideoDevice? _camera;
    
    private Dictionary<int, string> _faceNames = new Dictionary<int, string>();
    
    private static MainWindowViewModel ActiveWindow { get; set; }

    public async void EnableCamera()
    {

        IsCameraEnabled = true;
        
        using var _cameraManager = new CameraManager(DeviceInputFormat.AVFoundation);
        
        var options = new VideoInputOptions
        {
            VideoSize = new (640, 480),
            Framerate = new AVRational { num = 20, den = 1 },
            InputFormat = "rgba",
            IsRaw = true,
    
        };
        
        // Get the first camera available
        _camera = _cameraManager.GetDevice(0, options);
        
        // Attach your callback to the camera's frame event handler
        _camera.OnFrame += FrameHandler;
        
        _ = CaptureVideo();
        
    }
    
    private bool _isSaveEnabled ;
    
    
    private FisherFaceRecognizer _faceRecognizer;
    
    private Rectangle[] _faces;
    
    //private FaceLib.FaceRec faceRec =  new FaceLib.FaceRec();

    private async Task CaptureVideo()
    {
        
        // Start decoding frames asynchronously
        _camera.StartCapture();

        // Just wait a bit
        //Thread.Sleep(TimeSpan.FromSeconds(5));
        
    }

    public static void FrameHandler(object? _sender, FrameEventArgs e)
    {
        // Only care about new frames
        if (e.Status != DecodeStatus.NewFrame)
            return;
        
        Frame frame = e.Frame;

        ActiveWindow.ProcessImageAsync(frame);

    }

    public async Task DisableCamera()
    {
        // Stop processing:
        IsCameraEnabled = false;
        //Image = new Bitmap(AssetLoader.Open(new Uri("avares://FaceRec/Assets/placeholder.png")));
        
        _camera.StopCapture();
    }
    
    public void RecogStart()
    {
        IsRecognitionEnabled = true;
        
    }
    
    public void RecogStop()
    {
        IsRecognitionEnabled = false;
    }
    
    public async Task Init()
    {
        
        
        var index_file = Path.Combine(baseFolder, "index.txt");
        int i = 1;
                    
        if (File.Exists(index_file))
        {
            var lines = File.ReadAllLines(index_file);
            foreach (var line in lines)
            {
                var parts = line.Split(";");
                _faceNames.Add(i, parts[1]);
                i++;
            }

        }else
        {
            File.Create(Path.Combine(baseFolder, "index.txt"));
        }
        
        _faceRecognizer = new FisherFaceRecognizer(i, i);
        
        if (File.Exists(Path.Combine(baseFolder, "model.txt")))
        {
            _faceRecognizer.LoadModel(Path.Combine(baseFolder, "model.txt"));
        }
    }

    private async Task SaveIndex()
    {
        var fileName = Path.Combine(baseFolder, "index.txt");
        File.Delete(fileName);
        
        foreach (var faceName in _faceNames)
        {
            File.AppendAllText(fileName, $"{faceName.Key};{faceName.Value}\n");
        }
    }
    
    
    private async Task ProcessImageAsync(Frame frame)
    {
        //frameCount++;
        
        var converter = new FrameConverter(frame, PixelFormat.Rgba);
        var rgbaFrame = converter.Convert(frame);
        
        using var bitmap = LoadRGBAImage(rgbaFrame.RawData.ToArray(), rgbaFrame.Width, rgbaFrame.Height);


        if (IsRecognitionEnabled)
        {
            //var detector = new FaceRectangleDetector();
            //var retangles = detector.DetectFaceRectangles(bitmap);
            
            var dnnDetector = new FaceDetector();

            var faces = dnnDetector.Forward(new SkiaDrawing.Bitmap(bitmap));
            
            
            // Create a canvas to draw on the image
            using SKCanvas canvas = new SKCanvas(bitmap);
            
            /*
            // Create a paint brush with color and stroke
            using SKPaint paint = new SKPaint
            {
                Color = SKColors.Red,      // Rectangle color
                Style = SKPaintStyle.Stroke, // Stroke style (outline)
                StrokeWidth = 5           // Thickness of the border
            };

            foreach (var retangle in retangles)
            {
                // Draw the rectangle on the canvas
                canvas.DrawRect(retangle, paint);
            }
            */
            
            using SKPaint paint2 = new SKPaint
            {
                Color = SKColors.Yellow,      // Rectangle color
                Style = SKPaintStyle.Stroke, // Stroke style (outline)
                StrokeWidth = 5           // Thickness of the border
            };
            
            foreach (var face in faces)
            {
                // Draw the rectangle on the canvas
                canvas.DrawRect(face.Box.ToSKRect(), paint2);
            }
            
            // Only ID the first face

            if (faces.Length > 0)
            {
                var face = faces[0];
                var width = face.Box.Width;
                var height = face.Box.Height;
                        
                // Extract the sub-image
                using SKBitmap extractedPiece = new SKBitmap(width, height);
                        
                SKRectI region = new SKRectI(face.Box.X, face.Box.Y, face.Box.X + width, face.Box.Y + height);
                        
                bitmap.ExtractSubset(extractedPiece, region);
                        
                using SKBitmap grayscaleBitmap = ConvertToGrayscale(extractedPiece);
                        
                using var resizedBitmap = grayscaleBitmap.Resize(new SKImageInfo(100, 100), SKFilterQuality.High);
                
                var prediction = _faceRecognizer.Recognize(resizedBitmap);
                
                Identity = _faceNames[prediction];
                
            }
            


            if (_isSaveEnabled)
            {
                // Let's save the image to the disk
                
                // First, let's check if we have a name
                if(string.IsNullOrEmpty(PersonName))
                {
                    _= Dispatcher.UIThread.Invoke(async () =>
                    {
                        var box = MessageBoxManager
                            .GetMessageBoxStandard("ERRO", "Por favor entre com o nome da pessoa.",
                                ButtonEnum.Ok); 
                        
                        await box.ShowAsync();
                        
                    });

                }
                else
                {
                    int i = 1;
                    if(_faceNames.Count > 0) i = _faceNames.Keys.Max() + 1;
                    
                    foreach (var face in faces)
                    {
                        var width = face.Box.Width;
                        var height = face.Box.Height;
                        
                        // Extract the sub-image
                        using SKBitmap extractedPiece = new SKBitmap(width, height);
                        
                        SKRectI region = new SKRectI(face.Box.X, face.Box.Y, face.Box.X + width, face.Box.Y + height);
                        
                        bitmap.ExtractSubset(extractedPiece, region);
                        
                        using SKBitmap grayscaleBitmap = ConvertToGrayscale(extractedPiece);
                        
                        using var resizedBitmap =   grayscaleBitmap.Resize(new SKImageInfo(100, 100), SKFilterQuality.High);
                        
                        var destDir = Directory.CreateDirectory(Path.Combine(baseFolder, "faces"));
                        var destFile = Path.Combine(destDir.FullName, $"face_({PersonName})_{i}.png");
                        SaveBitmapToFile(resizedBitmap, destFile);
                        _faceNames.Add(i, PersonName);
                        i++;
                    }
                    
                    _= SaveIndex();
                    _ = TrainModel();
                }
                
                
                _isSaveEnabled = false;
            }
            
            
        }
        else
        {
            
        }
        
        // Convert SKBitmap to JPEG in MemoryStream
        using MemoryStream memoryStream = new MemoryStream();
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData encodedData = image.Encode(SKEncodedImageFormat.Jpeg, 90); // 90 = Quality

        // Write to memory stream
        encodedData.SaveTo(memoryStream);
        memoryStream.Position = 0;

        Image = new Bitmap(memoryStream);
        
        
        
    }
    
    private async Task TrainModel()
    {
        // Load the images
        var images = Directory.GetFiles(Path.Combine(baseFolder, "faces"), "*.png");

        var recognizer = new FisherFaceRecognizer(numComponentsPCA: images.Length, numComponentsLDA: images.Length);
        
        var indexes = _faceNames.Keys.ToArray();
        
        recognizer.Train(images, indexes);
        recognizer.SaveModel(Path.Combine(baseFolder, "model.txt"));
        
        _faceRecognizer = recognizer;
        
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
    
    
    /// <summary>
    /// Saves an SKBitmap to a file.
    /// </summary>
    private static void SaveBitmapToFile(SKBitmap bitmap, string filename)
    {
        using (SKImage image = SKImage.FromBitmap(bitmap))
        using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100)) // PNG format, quality 100
        using (FileStream stream = File.OpenWrite(filename))
        {
            data.SaveTo(stream);
        }
    }

    public static SKBitmap LoadRGBAImage(byte[] rgbaBytes, int width, int height)
    {
        if (rgbaBytes == null || rgbaBytes.Length != width * height * 4)
            throw new ArgumentException("Invalid byte array size for given dimensions.");

        SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);

        // Copy bytes into the bitmap's pixel buffer.
        //bitmap.InstallPixels(bitmap.Info, rgbaBytes, width * 4);
        

        // Pin the array so SkiaSharp can use it
        GCHandle handle = GCHandle.Alloc(rgbaBytes, GCHandleType.Pinned);
        IntPtr ptr = handle.AddrOfPinnedObject();

        // Install the pixels from the byte array
        if (!bitmap.InstallPixels(bitmap.Info, ptr, bitmap.Info.RowBytes, null, null))
        {
            Console.WriteLine("Failed to install pixels.");
        }

        // Free the pinned memory after use
        handle.Free();
        

        return bitmap;
    }


    public async Task SavePerson()
    {
        _isSaveEnabled = true;
    }
}
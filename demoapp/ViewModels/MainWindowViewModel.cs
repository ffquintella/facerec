using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FFmpeg.AutoGen;
using FlashCap;
using MLFaceLib;
using ReactiveUI;
using SeeShark;
using SeeShark.Decode;
using SeeShark.Device;
using SeeShark.FFmpeg;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using PixelFormat = SeeShark.PixelFormat;
using Rectangle = Avalonia.Controls.Shapes.Rectangle;

namespace demoapp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private int frameCount = 0;
    
    public string VideoRecognition  { get; } = "Sample Video Recognition APP";
    
    public MainWindowViewModel()
    {
        Image = new Bitmap(AssetLoader.Open(new Uri("avares://demoapp/Assets/placeholder.png")));
        
        // FFMPEG setup
        FFmpegManager.SetupFFmpeg (["/opt/homebrew/Cellar/ffmpeg/7.1_4/lib/"]);
        
        MainWindowViewModel.ActiveWindow = this;
        
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
    
    
    private async Task ProcessImageAsync(Frame frame)
    {
        //frameCount++;
        
        var converter = new FrameConverter(frame, PixelFormat.Rgba);
        var rgbaFrame = converter.Convert(frame);


        using var bitmap = LoadRGBAImage(rgbaFrame.RawData.ToArray(), rgbaFrame.Width, rgbaFrame.Height);


        if (IsRecognitionEnabled)
        {
            var detector = new FaceRectangleDetector();
                
            var retangles = detector.DetectFaceRectangles(bitmap);
            
            
            // Create a canvas to draw on the image
            using SKCanvas canvas = new SKCanvas(bitmap);
            

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
        
        
        /*using (Image<Rgba32> image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(rgbaFrame.RawData, rgbaFrame.Width, rgbaFrame.Height))
        {
            var ms = new MemoryStream();
            image.SaveAsJpeg(ms);
            ms.Position = 0;
            
            if (IsRecognitionEnabled)
            {
                var detector = new FaceDetector(100);
                
                var retangles = detector.DetectFaces(image);
            }
            else
            {
            }
            
            Image = new Bitmap(ms);
        }
        */
        
        
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
﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Platform;
using FaceONNX;
using FlashCap;
using MLFaceLib;
using MLFaceLib.ImageTools;
using MLFaceLib.ONNX;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using SkiaSharp;
using UMapx.Core;
using PixelFormats = FlashCap.PixelFormats;
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
        
        ActiveWindow = this;

        //_ = Init();

    }
    
    
    private bool _captureColorAnalysis;
    
    public bool CaptureColorAnalysis
    {
        get => _captureColorAnalysis;
        set => this.RaiseAndSetIfChanged(ref _captureColorAnalysis, value);
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
    
    private IBrush _backgroundBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
    
    public IBrush BackgroundBrush
    {
        get => _backgroundBrush;
        set => this.RaiseAndSetIfChanged(ref _backgroundBrush, value);
    }

    private string _r;
    
    public string R
    {
        get => _r;
        set => this.RaiseAndSetIfChanged(ref _r, value);
    }
    
    private string _g;
    
    public string G
    {
        get => _g;
        set => this.RaiseAndSetIfChanged(ref _g, value);
    }
    
    private string _b;
    
    public string B
    {
        get => _b;
        set => this.RaiseAndSetIfChanged(ref _b, value);
    }

    private string _w;
    
    public string W
    {
        get => _w;
        set => this.RaiseAndSetIfChanged(ref _w, value);
    }
    
    
    private string _nc;
    
    public string NC
    {
        get => _nc;
        set => this.RaiseAndSetIfChanged(ref _nc, value);
    }
    
    private Bitmap _image;
    
    public Bitmap Image
    {
        get => _image;
        set => this.RaiseAndSetIfChanged(ref _image, value);
    }
    
    private SKImage _skImage;
    
    private int _width = 640;
    
    public int Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }
    
    private int _height = 640;
    
    public int Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }
    
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
    
    private string _source = "---";
    
    public string Source
    {
        get => _source;
        set => this.RaiseAndSetIfChanged(ref _source, value);
    }
    
    private string _similarity;
    
    public string Similarity
    {
        get => _similarity;
        set => this.RaiseAndSetIfChanged(ref _similarity, value);
    }
    
    private string _identity;
    
    public string Identity
    {
        get => _identity;
        set => this.RaiseAndSetIfChanged(ref _identity, value);
    }
    
    private string _confidence;
    
    public string Confidence
    {
        get => _confidence;
        set => this.RaiseAndSetIfChanged(ref _confidence, value);
    }

    
    
    private CaptureDeviceDescriptor? _device;
    
    public CaptureDeviceDescriptor? Device
    {
        get => _device;
        set => this.RaiseAndSetIfChanged(ref _device, value);
    }
    
    private ObservableCollection<CaptureDeviceDescriptor> _deviceList;
    
    public ObservableCollection<CaptureDeviceDescriptor> DeviceList
    {
        get => _deviceList;
        set => this.RaiseAndSetIfChanged(ref _deviceList, value);
    }
    
    private ObservableCollection<VideoCharacteristics> _characteristicsList;
    
    public ObservableCollection<VideoCharacteristics> CharacteristicsList
    {
        get => _characteristicsList;
        set => this.RaiseAndSetIfChanged(ref _characteristicsList, value);
    }

    private VideoCharacteristics? _characteristics;
    
    public VideoCharacteristics? Characteristics
    {
        get => _characteristics;
        set => this.RaiseAndSetIfChanged(ref _characteristics, value);
    }
    
    // Constructed capture device.
    private CaptureDevice? captureDevice;
    
    private static MainWindowViewModel ActiveWindow { get; set; }
    
    private ColorIdentifier colorIdentifier = new ColorIdentifier();
    
    private int colorIndex = 0;



    public async Task EnableCamera()
    {

        IsCameraEnabled = true;
        
        // Descriptor is assigned and set valid characteristics:
        if (this.Device is { } descriptor &&
            Characteristics is { })
        {
            
            // Open capture device:
            Debug.WriteLine($"OnCharacteristicsChangedAsync: Opening: {descriptor.Name}");
            this.captureDevice = await descriptor.OpenAsync(
                Characteristics,
                this.OnPixelBufferArrivedAsync);
            
        }
        
        await CaptureVideo();
        
    }
    
    private bool _isSaveEnabled ;
    
    private FaceRecognizer _faceRecognizer;
    
    private FaceDetectionResult[] _faces;

    private async Task CaptureVideo()
    {
        
        // Start decoding frames asynchronously
        await this.captureDevice!.StartAsync();
        
    }
    

    public async Task DisableCamera()
    {
        await captureDevice!.StopAsync();
        
        // Stop processing:
        IsCameraEnabled = false;
        
        Dispatcher.UIThread.Post(() =>
        Image = new Bitmap(AssetLoader.Open(new Uri("avares://demoapp/Assets/placeholder.png"))));
    }
    
    public void RecogStart()
    {
        IsRecognitionEnabled = true;
        
    }
    
    public void RecogStop()
    {
        IsRecognitionEnabled = false;
    }

    public async Task CaptureColor(bool captured = false)
    {
        if (captured)
        {
            colorIndex++;
        }
        
        switch (colorIndex)
        {
            case 0:
                await Dispatcher.UIThread.InvokeAsync(() => BackgroundBrush = new SolidColorBrush(Color.FromRgb(255,0,0)));
                break;
            case 1:
                Dispatcher.UIThread.Post(() => BackgroundBrush = new SolidColorBrush(Color.FromRgb(0,255,0)));
                break;
            case 2:
                Dispatcher.UIThread.Post(() => BackgroundBrush = new SolidColorBrush(Color.FromRgb(0,0,255)));
                break;
            case 3:
                Dispatcher.UIThread.Post(() => BackgroundBrush = new SolidColorBrush(Color.FromRgb(255,255,255)));
                break;
            case 4:
                Dispatcher.UIThread.Post(() => BackgroundBrush = new SolidColorBrush(Color.FromRgb(0,0,0)));
                break;
        }

        //Thread.Sleep(10);
        

        if (colorIndex > 5)
        {
            colorIndex = 0;
            CaptureColorAnalysis = false;
        }
        else
        {
            CaptureColorAnalysis = true;
        }
    }
    
    public async Task ColorVerify()
    {
        _= CaptureColor();
    }
    
    public async Task OnLoaded()
    {
        await Init();
    }
    
    public async Task Init()
    {
        var devices = new CaptureDevices();
        
        DeviceList = new ObservableCollection<CaptureDeviceDescriptor>();
        try
        {
            int t = 0;

            IEnumerable<CaptureDeviceDescriptor> descriptors = new CaptureDeviceDescriptor[0];
            
            while( t < 10)
            {
                descriptors = devices.EnumerateDescriptors();
                if(descriptors.Any()) break;
                t++;
                Thread.Sleep(1000);
            }
            
            if( descriptors is null) throw new Exception("Could not find any devices");
            
            //var descriptors = devices.EnumerateDescriptors();
            
            Console.WriteLine($"Found {descriptors.Count()} devices");
            
            foreach (var descriptor in descriptors.
                         // You could filter by device type and characteristics.
                         //Where(d => d.DeviceType == DeviceTypes.DirectShow).  // Only DirectShow device.
                         Where(d => d.Characteristics.Length >= 1))             // One or more valid video characteristics.
            {
                DeviceList.Add(descriptor);
            }
        
            Device = DeviceList.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        if(Device == null) throw new Exception("No Device Found");
        
        CharacteristicsList = new ObservableCollection<VideoCharacteristics>();
        foreach (var characteristics in Device.Characteristics.Where(c => c.PixelFormat == PixelFormats.ARGB32))
        {
            if (characteristics.PixelFormat !=  PixelFormats.Unknown)
            {
                this.CharacteristicsList.Add(characteristics);
            }
        }

        if (CharacteristicsList is null or { Count: <= 0 }) throw new Exception("Invalid camera");
        
        Characteristics = CharacteristicsList.FirstOrDefault(c => c is { Width: > 900 });

        Height = Characteristics!.Height;
        Width = Characteristics!.Width;
        
        
        _faceRecognizer = new FaceRecognizer();
        
        if (File.Exists(Path.Combine(baseFolder, "facedata.fdt")))
        {
            _faceRecognizer.Load(Path.Combine(baseFolder, "facedata.fdt"));
        }
        else
        {
            if(Directory.Exists(Path.Combine(baseFolder, "faces")))
            {
                _ = TrainModel();
            }
        }
    }

    private async Task OnPixelBufferArrivedAsync(PixelBufferScope bufferScope)
    {
        // Or, refer image data binary directly.
        ArraySegment<byte> imageSegment = bufferScope.Buffer.ReferImage();
        
        SKImageInfo desiredInfo = new SKImageInfo(1280, 960, SKColorType.Bgra8888, SKAlphaType.Premul);
        
        Width = 1280;
        Height = 960;
        
        if (imageSegment.Count - 54 < desiredInfo.BytesSize)
            throw new ArgumentException("RGB buffer is too small for the given dimensions.");

        // Allocate RGBA output buffer
        byte[] rgbaBytes = new byte[desiredInfo.BytesSize];
        
        int offset = imageSegment.Offset + 54; // Skip BMP header

        for (int i = 0, j = 0; i < desiredInfo.BytesSize; i += 4)
        {
            
            // This is in ARGB format
            var X = imageSegment.Array[offset + i + 0];
            var Y = imageSegment.Array[offset + i + 1];
            var Z = imageSegment.Array[offset + i + 2];
            var W = imageSegment.Array[offset + i + 3];


            rgbaBytes[i + 0] = X;// B
            rgbaBytes[i + 1] = Y; // G
            rgbaBytes[i + 2] = Z; // R
            rgbaBytes[i + 3] = W; // A
        }
        
        
        
        //SKBitmap bitmap = new SKBitmap();
        SKImage? image;
        GCHandle handle = GCHandle.Alloc(rgbaBytes, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            using (SKPixmap pixmap = new SKPixmap(desiredInfo, ptr, desiredInfo.RowBytes))
            {
                image = SKImage.FromPixels(pixmap);
            }
        }
        finally
        {
            handle.Free();
        }
        
        
        // `bitmap` is copied, so we can release pixel buffer now.
        bufferScope.ReleaseNow();
        
        if(image == null) throw new System.Exception("No image");
        
        await ProcessImageAsync(image);
    }


    private async Task ProcessImageAsync(SKImage skImage)
    {
        
        SKImage? markedImage = null;
        
        if(frameCount > 1000)
        {
            frameCount = 0;
            //GC.Collect();
        }
        frameCount++;
        

        if (IsRecognitionEnabled)
        {
            var bitmap = SKBitmap.FromImage(skImage);
            
            if(frameCount % 20 == 0) _= Task.Run(() =>
            {
                var dnnDetector = new FaceDetector();
                _faces = dnnDetector.Forward(new SkiaDrawing.Bitmap(bitmap));
            });
            
            // Only ID the first face
            if (_faces.Length > 0)
            {
                
                // Create a canvas to draw on the image
                //using SKCanvas canvas = new SKCanvas(bitmap);
                using SKSurface surface = SKSurface.Create(skImage.PeekPixels());
                using SKCanvas canvas = surface.Canvas;
            
                using SKPaint paintYellow = new SKPaint
                {
                    Color = SKColors.Yellow,      // Rectangle color
                    Style = SKPaintStyle.Stroke, // Stroke style (outline)
                    StrokeWidth = 5           // Thickness of the border
                };
            
                foreach (var faceRec in _faces)
                {
                    // Draw the rectangle on the canvas
                    canvas.DrawRect(faceRec.Box.ToSKRect(), paintYellow);
                }
                
                markedImage = surface.Snapshot();
                
                var face = _faces[0];
                var width = face.Box.Width;
                var height = face.Box.Height;
                        
                // Extract the sub-image
                using SKBitmap extractedPiece = new SKBitmap(width, height);
                        
                SKRectI region = new SKRectI(face.Box.X, face.Box.Y, face.Box.X + width, face.Box.Y + height);
                        
                bitmap.ExtractSubset(extractedPiece, region);

                var idImage = extractedPiece.Copy();
                
                _= Task.Run(async () =>
                {
                    if (frameCount % 60 == 0)
                    {
                    
                        var prediction = await _faceRecognizer.Predict(idImage);
                        
                        idImage.Dispose();
                
                        Confidence = prediction.Item4.ToString(CultureInfo.CurrentCulture);
                        Similarity = prediction.Item2.ToString(CultureInfo.CurrentCulture);

                        var fconf = prediction.Item4;

                        if (prediction.Item1 is null)
                        {
                            Identity = "Desconhecido";
                        }
                        else Identity = prediction.Item1;
                        
                        if(fconf < 1) Source = "Real";
                        else Source = "Fake";
  
                    }
                });



                if (CaptureColorAnalysis)
                {
                    CaptureColorAnalysis = false;
                    await Task.Run(async () =>
                    {
                        var colorPredictor = colorIdentifier;
                    
                        //var normalizedArray = NormalizationHelper.RGBMeanNormalization(idImage,
                        //    [0.485f, 0.456f, 0.406f], [0.229f, 0.224f, 0.225f]);
                        
                        var filePath = "/Users/felipe/tmp/";
                        using (SKImage image = SKImage.FromBitmap(idImage))
                        using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100)) // PNG format, quality 100
                        using (FileStream stream = File.OpenWrite(filePath + $"{colorIndex}_sample.png"))
                        {
                            data.SaveTo(stream);
                        }
                        
                        
                        var cpfloat = colorPredictor.Forward(idImage);

                        var max = Matrice.Max(cpfloat, out int cPredict);
                        var cLabel = ColorIdentifier.Labels[cPredict];

                        if (cLabel == "NC") NC = $"{colorIndex}";  // 1 - W / NC  4 - R
                        else if (cLabel == "R") R = $"{colorIndex}";
                        else if (cLabel == "G") G = $"{colorIndex}";
                        else if (cLabel == "B") B = $"{colorIndex}";
                        else if (cLabel == "W") W = $"{colorIndex}";

                        await CaptureColor(true);
                        
                    });

                    
                }
                
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
                    // Since we only have one name just save the first face
                    int i = 1;
                    var face = _faces[0];
                    
                    var width = face.Box.Width;
                    var height = face.Box.Height;
                    
                    // Extract the sub-image
                    using SKBitmap extractedPiece = new SKBitmap(width, height);
                    
                    SKRectI region = new SKRectI(face.Box.X, face.Box.Y, face.Box.X + width, face.Box.Y + height);
                    
                    bitmap.ExtractSubset(extractedPiece, region);
                    
                    using SKBitmap grayscaleBitmap = ConvertToGrayscale(extractedPiece);
                    
                    using var resizedBitmap =   grayscaleBitmap.Resize(new SKImageInfo(900, 900), SKFilterQuality.High);
                    
                    var destDir = Directory.CreateDirectory(Path.Combine(baseFolder, "faces"));
                    
                    var destFile = Path.Combine(destDir.FullName, $"face_({PersonName})_{i}.png");
                    
                    while(File.Exists(destFile))
                    {
                        i++;
                        destFile = Path.Combine(destDir.FullName, $"face_({PersonName})_{i}.png");
                    }
                    
                    SaveBitmapToFile(resizedBitmap, destFile);
                    
                    _ = TrainModel();
                }
                
                _isSaveEnabled = false;
            }
            
        }
        else
        {
            
        }
        
        // Convert SKBitmap to JPEG in MemoryStream
        try
        {
            SKData? encodedData = null;
            if(markedImage != null) encodedData = skImage.Encode(SKEncodedImageFormat.Jpeg, 90);
            else encodedData = skImage.Encode(SKEncodedImageFormat.Jpeg, 90);

            
            using MemoryStream memoryStream = new MemoryStream();
            // Write to memory stream
            encodedData.SaveTo(memoryStream);
            memoryStream.Position = 0;

            Image = new Bitmap(memoryStream);
            encodedData?.Dispose();
            
        }catch(Exception ex)
        {
            Console.WriteLine(@"Error converting image: " + ex.Message);
        }
        
    }
    
    private async Task TrainModel()
    {
        
        _faceRecognizer.Clear();
        
        await _faceRecognizer.TrainAsync(Path.Combine(baseFolder, "faces"));
        
        await _faceRecognizer.SaveModel(Path.Combine(baseFolder, "facedata.fdt"));
        
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
    
    public async Task Dispose()
    {
        await captureDevice!.StopAsync();
        captureDevice?.Dispose();
    }
    
}
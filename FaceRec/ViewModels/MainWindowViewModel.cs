using System.Drawing;
using FlashCap;
using System.Reactive;
using Avalonia.Platform;
using FaceLib;
using FaceRec.Helpers;
using ReactiveUI;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using PixelFormats = FlashCap.PixelFormats;


namespace FaceRec.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    
    private int frameCount = 0;
    
    public string VideoRecognition  { get; } = "Sample Video Recognition APP";
    
    public MainWindowViewModel()
    {
        Image = new Bitmap(AssetLoader.Open(new Uri("avares://FaceRec/Assets/placeholder.png")));
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
    
    private string _personName;
    
    public string PersonName
    {
        get => _personName;
        set => this.RaiseAndSetIfChanged(ref _personName, value);
    }

    private CaptureDevice? _device;

    public async void EnableCamera()
    {

        IsCameraEnabled = true;

        _ = CaptureVideo();
        
    }
    
    private bool _isSaveEnabled ;
    
    
    private Rectangle[] _faces;

    private async Task CaptureVideo()
    {
        // Enable Camera
        
        // Capture device enumeration:
        var devices = new CaptureDevices();

        foreach (var descriptor in devices.EnumerateDescriptors())
        {
            // "Logicool Webcam C930e: DirectShow device, Characteristics=34"
            // "Default: VideoForWindows default, Characteristics=1"
            Console.WriteLine(descriptor);

            foreach (var characteristics in descriptor.Characteristics)
            {
                // "1920x1080 [JPEG, 30fps]"
                // "640x480 [YUYV, 60fps]"
                Console.WriteLine(characteristics);
                if(characteristics.PixelFormat == PixelFormats.Unknown)
                    Console.WriteLine("Unsupported format");
            }
        }

        
        // Open a device with a video characteristics:
        var descriptor0 = devices.EnumerateDescriptors().ElementAt(0);
        
        
        // Exclude unsupported formats:
        var characteristicsSup = descriptor0.Characteristics.
            Where(c => c.PixelFormat != PixelFormats.Unknown).
            ToArray();
        
        
        await using var device = await descriptor0.OpenAsync(
            characteristicsSup[4],
            //TranscodeFormats.BT709,
            //true,
            //10,
            async bufferScope => await ProcessImageAsync(bufferScope));
        
        
        /*using var deviceObservable = await descriptor0.AsObservableAsync(
            characteristicsSup[0],
        TranscodeFormats.BT709);*/

        // Subscribe the device.
        //deviceObservable.Subscribe(bufferScope => ProcessImageAsync(bufferScope));

        // Start processing:
        await device.StartAsync();

        await Task.Run(async () =>
        {
            while(IsCameraEnabled) await Task.Delay(new TimeSpan(0, 0, 1));
            
        });
        
        await device.StopAsync();
        Image = new Bitmap(AssetLoader.Open(new Uri("avares://FaceRec/Assets/placeholder.png")));
    }

    public async Task DisableCamera()
    {
        // Stop processing:
        IsCameraEnabled = false;
        //Image = new Bitmap(AssetLoader.Open(new Uri("avares://FaceRec/Assets/placeholder.png")));
    }
    
    public void RecogStart()
    {
        IsRecognitionEnabled = true;
    }
    
    public void RecogStop()
    {
        IsRecognitionEnabled = false;
    }
    
    
    private async Task ProcessImageAsync(PixelBufferScope bufferScope)
    {
        frameCount++;
        
        // here executed in a worker thread
        //byte[] imageData = bufferScope.Buffer.ExtractImage();
        
        //byte[] imageData = bufferScope.Buffer.CopyImage();
        
        ArraySegment<byte> image =
            bufferScope.Buffer.ReferImage();
        
        // Anything use of it...
        //var ms = new MemoryStream(imageData);


        if (IsRecognitionEnabled)
        {
            var faceRec = new FaceLib.FaceRec();
        
            //Image = new Bitmap(ms);
        
            if(frameCount % 4 == 0)
                _faces = faceRec.DetectFace(image.Array);
        
            //Console.WriteLine("Frame Count: " + frameCount);
        
            if(frameCount > 100) frameCount = 0;


            if (_faces.Length > 0)
            {
                if (_isSaveEnabled)
                {
                    faceRec.SaveFace(image.Array, _faces[0], PersonName);
                    _isSaveEnabled = false;
                }
                
                Image = ImageDraw.DrawRectanglesOnFaces(image.Array, _faces);
            }
            else
            {
                var ms = new MemoryStream(
                    image.Array, image.Offset, image.Count);
                Image = new Bitmap(ms);
            }
        }
        else
        {
            var ms = new MemoryStream(
                image.Array, image.Offset, image.Count);
            Image = new Bitmap(ms);
        }


    }

    public async Task SavePerson()
    {
        _isSaveEnabled = true;
    }

}
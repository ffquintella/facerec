using FlashCap;
using System.Reactive;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FaceRec.Helpers;
using ReactiveUI;
using PixelFormats = FlashCap.PixelFormats;


namespace FaceRec.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
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
    
    private Bitmap _image;
    
    public Bitmap Image
    {
        get => _image;
        set => this.RaiseAndSetIfChanged(ref _image, value);
    }

    private CaptureDevice? _device;

    public async void EnableCamera()
    {

        IsCameraEnabled = true;

        _ = CaptureVideo();
        
    }

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
            characteristicsSup[0],
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
    
    
    private async Task ProcessImageAsync(PixelBufferScope bufferScope)
    {
        // here executed in a worker thread
        //byte[] imageData = bufferScope.Buffer.ExtractImage();
        
        //byte[] imageData = bufferScope.Buffer.CopyImage();
        
        ArraySegment<byte> image =
            bufferScope.Buffer.ReferImage();
        
        // Anything use of it...
        //var ms = new MemoryStream(imageData);
        
        var ms = new MemoryStream(
            image.Array, image.Offset, image.Count);

        var faceRec = new FaceLib.FaceRec();

        //var imgBytes = ms.ToArray();
        
        Image = new Bitmap(ms);
        /*
        using (var stream = new MemoryStream())
        {
            Image.Save(stream);
            var faces = faceRec.DetectFace(stream, Convert.ToInt32(Image.Size.Width), Convert.ToInt32(Image.Size.Height));
        }*/


    }
    
}
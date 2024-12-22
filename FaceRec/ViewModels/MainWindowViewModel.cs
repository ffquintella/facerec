using FlashCap;
using System.Reactive;
using Avalonia.Media.Imaging;
using FaceRec.Helpers;
using ReactiveUI;


namespace FaceRec.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string VideoRecognition  { get; } = "Sample Video Recognition APP";
    
    public MainWindowViewModel()
    {
    }
    
    
    private Bitmap _image;
    
    public Bitmap Image
    {
        get => _image;
        set => this.RaiseAndSetIfChanged(ref _image, value);
    }

    public async void EnableCamera()
    {

        //Image = ImageHelper.LoadFromDisk("E:\\tmp\\blackbuck.bmp");
        
        
        
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
            }
        }

        // Open a device with a video characteristics:
        var descriptor0 = devices.EnumerateDescriptors().ElementAt(0);

        await using var device = await descriptor0.OpenAsync(
            descriptor0.Characteristics[0],
            async bufferScope => await ProcessImageAsync(bufferScope));

        // Start processing:
        await device.StartAsync();

        // ...

        // Stop processing:
        await device.StopAsync();
        
        
    }
    
    private async Task ProcessImageAsync(PixelBufferScope bufferScope)
    {
        // here executed in a worker thread
        byte[] imageData = bufferScope.Buffer.ExtractImage();
        
        // Anything use of it...
        var ms = new MemoryStream(imageData);

        Image = new Bitmap(ms);

        //Image = System.Drawing.Bitmap.FromStream(ms);



    }
    
}
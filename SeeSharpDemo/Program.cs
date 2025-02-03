// See https://aka.ms/new-console-template for more information

using FFmpeg.AutoGen;
using SeeShark;
using SeeShark.Device;
using SeeShark.FFmpeg;
using SeeSharpDemo;

Console.WriteLine("Camera Hello, World!");

/*FFmpegManager.SetupFFmpeg(["/opt/homebrew/Cellar/ffmpeg/7.1_4/lib/",
    "/opt/homebrew/Cellar/ffmpeg/7.1_4/lib",
    "/opt/homebrew/bin/ffmpeg", 
    "/opt/homebrew/bin/", "/opt/homebrew/share/ffmpeg", 
    "/opt/homebrew/Cellar/ffmpeg/7.1_4/bin/"]);*/

FFmpegManager.SetupFFmpeg(["/opt/homebrew/Cellar/ffmpeg/7.1_4/lib/"]);

//FFmpegManager.SetupFFmpeg(["/opt/homebrew/Cellar/ffmpeg@6/6.1.2_7/lib/"]);

// Create a CameraManager to manage camera devices
using var manager = new CameraManager(DeviceInputFormat.AVFoundation);

var options = new VideoInputOptions
{
    VideoSize = new (640, 480),
    Framerate = new AVRational { num = 20, den = 1 },
    InputFormat = "rgba",
    IsRaw = true,
    
};

// Get the first camera available
using var camera = manager.GetDevice(0, options);

// Attach your callback to the camera's frame event handler
camera.OnFrame += FrameHandler.Handler;

// Start decoding frames asynchronously
camera.StartCapture();

// Just wait a bit
Thread.Sleep(TimeSpan.FromSeconds(20));

// Stop decoding frames
camera.StopCapture();
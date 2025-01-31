// See https://aka.ms/new-console-template for more information

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

// Create a CameraManager to manage camera devices
using var manager = new CameraManager();

// Get the first camera available
using var camera = manager.GetDevice(0);

// Attach your callback to the camera's frame event handler
camera.OnFrame += FrameHandler.Handler;

// Start decoding frames asynchronously
camera.StartCapture();

// Just wait a bit
Thread.Sleep(TimeSpan.FromSeconds(10));

// Stop decoding frames
camera.StopCapture();
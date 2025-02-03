

using SeeShark;
using SeeShark.Decode;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SeeSharpDemo;

public static class FrameHandler
{
    public static void Handler(object? _sender, FrameEventArgs e)
    {
        // Only care about new frames
        if (e.Status != DecodeStatus.NewFrame)
            return;

        Frame frame = e.Frame;

        // Get information and raw data from a frame
        Console.WriteLine($"New frame ({frame.Width}x{frame.Height} | {frame.PixelFormat})");
        Console.WriteLine($"Length of raw data: {frame.RawData.Length} bytes");
        
        // Do something with the frame
        Console.WriteLine("Writing frame to file...");
        
        var converter = new FrameConverter(frame, PixelFormat.Rgba);

        var rgbaFrame = converter.Convert(frame);
        
        using (Image<Rgba32> image = Image.LoadPixelData<Rgba32>(rgbaFrame.RawData, rgbaFrame.Width, rgbaFrame.Height))
        {
            image.SaveAsJpeg($"/Users/felipe/tmp/frame_{DateTime.Now:yyyyMMddHHmmss}.jpg");
        }
        
        //File.WriteAllBytes($"/Users/felipe/tmp/frame_{DateTime.Now:yyyyMMddHHmmss}.raw", frame.RawData);
        
        
        
        
    }
}
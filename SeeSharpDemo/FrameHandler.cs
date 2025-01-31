using SeeShark;
using SeeShark.Decode;

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
    }
}
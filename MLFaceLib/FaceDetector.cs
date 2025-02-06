namespace MLFaceLib;
using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;


/// <summary>
/// A heuristic face detector that returns bounding rectangles (covering whole faces)
/// using skin–color segmentation, morphological operations, and hole filling.
/// This version is tuned to be more robust for faces with beards or glasses.
/// </summary>
public class FaceRectangleDetector
{
    /// <summary>
    /// Determines whether the given color is “skin–colored.”
    /// The thresholds are relaxed slightly so that darker skin regions,
    /// or parts of the face obscured by beard or glasses, are less likely to be rejected.
    /// </summary>
    private bool IsSkinColor(SKColor color)
    {
        // Convert to a YCrCb–like space.
        double r = color.Red;
        double g = color.Green;
        double b = color.Blue;
        double Y = 0.299 * r + 0.587 * g + 0.114 * b;
        double Cr = (r - Y) * 0.713 + 128;
        double Cb = (b - Y) * 0.564 + 128;

        // Relax thresholds to be a bit more inclusive.
        return (Cr >= 130 && Cr <= 185 && Cb >= 80 && Cb <= 140);
    }

    /// <summary>
    /// Dilates a binary mask using a square kernel.
    /// </summary>
    private bool[,] DilateMask(bool[,] mask, int width, int height, int kernelSize = 3)
    {
        bool[,] result = new bool[width, height];
        int offset = kernelSize / 2;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (mask[x, y])
                {
                    for (int dy = -offset; dy <= offset; dy++)
                    {
                        for (int dx = -offset; dx <= offset; dx++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                result[nx, ny] = true;
                            }
                        }
                    }
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Erodes a binary mask using a square kernel.
    /// </summary>
    private bool[,] ErodeMask(bool[,] mask, int width, int height, int kernelSize = 3)
    {
        bool[,] result = new bool[width, height];
        int offset = kernelSize / 2;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool keep = true;
                for (int dy = -offset; dy <= offset && keep; dy++)
                {
                    for (int dx = -offset; dx <= offset; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height || !mask[nx, ny])
                        {
                            keep = false;
                            break;
                        }
                    }
                }
                result[x, y] = keep;
            }
        }
        return result;
    }

    /// <summary>
    /// Applies morphological closing (dilation then erosion) to help join fragmented skin areas.
    /// The kernel size is increased so that gaps (e.g. in the beard region or over glasses) can be filled.
    /// </summary>
    private bool[,] MorphologicalClosing(bool[,] mask, int width, int height, int iterations = 1, int kernelSize = 5)
    {
        bool[,] result = mask;
        for (int i = 0; i < iterations; i++)
        {
            result = DilateMask(result, width, height, kernelSize);
        }
        for (int i = 0; i < iterations; i++)
        {
            result = ErodeMask(result, width, height, kernelSize);
        }
        return result;
    }

    /// <summary>
    /// Fills in “holes” (small regions of false within a true blob) that might be caused
    /// by beards or glasses. Only holes not connected to the image border and smaller than
    /// maxHoleSize are filled.
    /// </summary>
    private bool[,] FillHoles(bool[,] mask, int width, int height, int maxHoleSize = 1000)
    {
        bool[,] filled = (bool[,])mask.Clone();
        bool[,] visited = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Process only background pixels that haven't been visited.
                if (!filled[x, y] && !visited[x, y])
                {
                    List<(int x, int y)> region = new List<(int, int)>();
                    bool touchesBorder = false;
                    Queue<(int x, int y)> queue = new Queue<(int, int)>();
                    queue.Enqueue((x, y));
                    visited[x, y] = true;

                    while (queue.Count > 0)
                    {
                        var (cx, cy) = queue.Dequeue();
                        region.Add((cx, cy));
                        if (cx == 0 || cy == 0 || cx == width - 1 || cy == height - 1)
                            touchesBorder = true;

                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = cx + dx;
                                int ny = cy + dy;
                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    if (!filled[nx, ny] && !visited[nx, ny])
                                    {
                                        visited[nx, ny] = true;
                                        queue.Enqueue((nx, ny));
                                    }
                                }
                            }
                        }
                    }
                    // If the region is enclosed (does not touch the border) and small enough, fill it.
                    if (!touchesBorder && region.Count <= maxHoleSize)
                    {
                        foreach (var pt in region)
                        {
                            filled[pt.x, pt.y] = true;
                        }
                    }
                }
            }
        }
        return filled;
    }

    /// <summary>
    /// Finds connected components (blobs) in the binary mask.
    /// Only components with an area equal to or greater than minArea are returned.
    /// </summary>
    private List<List<(int x, int y)>> FindConnectedComponents(bool[,] mask, int width, int height, int minArea)
    {
        bool[,] visited = new bool[width, height];
        List<List<(int x, int y)>> components = new List<List<(int x, int y)>>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (mask[x, y] && !visited[x, y])
                {
                    List<(int x, int y)> component = new List<(int, int)>();
                    Queue<(int x, int y)> queue = new Queue<(int, int)>();
                    queue.Enqueue((x, y));
                    visited[x, y] = true;

                    while (queue.Count > 0)
                    {
                        var pt = queue.Dequeue();
                        component.Add(pt);
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = pt.x + dx;
                                int ny = pt.y + dy;
                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    if (mask[nx, ny] && !visited[nx, ny])
                                    {
                                        visited[nx, ny] = true;
                                        queue.Enqueue((nx, ny));
                                    }
                                }
                            }
                        }
                    }
                    if (component.Count >= minArea)
                        components.Add(component);
                }
            }
        }
        return components;
    }

    /// <summary>
    /// Processes the input SKBitmap to detect candidate face regions.
    /// Returns a list of SKRect objects that (hopefully) cover whole faces.
    /// </summary>
    public List<SKRect> DetectFaceRectangles(SKBitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        // 1. Build the initial skin–color mask.
        bool[,] skinMask = new bool[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SKColor color = bitmap.GetPixel(x, y);
                skinMask[x, y] = IsSkinColor(color);
            }
        }

        // 2. Apply morphological closing with a larger kernel to join fragmented regions.
        bool[,] closedMask = MorphologicalClosing(skinMask, width, height, iterations: 2, kernelSize: 5);

        // 3. Fill small holes that may occur due to beards or glasses.
        bool[,] filledMask = FillHoles(closedMask, width, height, maxHoleSize: 1000);

        // 4. Extract connected components.
        var components = FindConnectedComponents(filledMask, width, height, minArea: 1500);

        List<SKRect> faceRects = new List<SKRect>();
        foreach (var comp in components)
        {
            int minX = width, minY = height, maxX = 0, maxY = 0;
            foreach (var pt in comp)
            {
                if (pt.x < minX) minX = pt.x;
                if (pt.y < minY) minY = pt.y;
                if (pt.x > maxX) maxX = pt.x;
                if (pt.y > maxY) maxY = pt.y;
            }
            // Expand the rectangle by a margin (10% of its dimensions)
            int rectWidth = maxX - minX;
            int rectHeight = maxY - minY;
            int marginX = (int)(rectWidth * 0.1);
            int marginY = (int)(rectHeight * 0.1);
            minX = Math.Max(0, minX - marginX);
            minY = Math.Max(0, minY - marginY);
            maxX = Math.Min(width - 1, maxX + marginX);
            maxY = Math.Min(height - 1, maxY + marginY);

            // Optionally filter out regions that do not conform to expected face aspect ratios.
            float aspectRatio = (float)(maxX - minX) / (maxY - minY);
            if (aspectRatio < 0.5f || aspectRatio > 1.5f)
                continue;
            if ((maxX - minX) < 60 || (maxY - minY) < 60)
                continue;

            faceRects.Add(new SKRect(minX, minY, maxX, maxY));
        }

        return faceRects;
    }
}






namespace MLFaceLib.HaarCascadeDetection;

public static class IntegralHelper
{

    /// <summary>
    /// Computes the standard integral image from a source image.
    /// The source image is given as a 2D long array.
    /// </summary>
    /// <param name="src">The source image (grayscale values) as a 2D long array.</param>
    /// <returns>A 2D long array representing the standard integral image.</returns>
    public static long[,] ComputeStandardIntegralImage(long[,] src)
    {
        int width = src.GetLength(0);
        int height = src.GetLength(1);
        long[,] integral = new long[width, height];

        for (int y = 0; y < height; y++)
        {
            long rowSum = 0;
            for (int x = 0; x < width; x++)
            {
                rowSum += src[x, y];
                if (y == 0)
                    integral[x, y] = rowSum;
                else
                    integral[x, y] = integral[x, y - 1] + rowSum;
            }
        }
        return integral;
    }

    /// <summary>
    /// Computes the rotated (tilted) integral image from a source image.
    /// The source image is given as a 2D long array.
    /// This implementation uses the recurrence:
    /// 
    ///     T(x,y) = f(x,y) + T(x-1,y-1) + T(x+1,y-1) - T(x,y-2)
    /// 
    /// with boundary checks (indices outside the image are treated as zero).
    /// </summary>
    /// <param name="src">The source image as a 2D long array.</param>
    /// <returns>A 2D long array representing the rotated integral image.</returns>
    public static long[,] ComputeRotatedIntegralImage(long[,] src)
    {
        int width = src.GetLength(0);
        int height = src.GetLength(1);
        long[,] tilted = new long[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                long fVal = src[x, y];
                long t1 = (x - 1 >= 0 && y - 1 >= 0) ? tilted[x - 1, y - 1] : 0;
                long t2 = (x + 1 < width && y - 1 >= 0) ? tilted[x + 1, y - 1] : 0;
                long t3 = (y - 2 >= 0) ? tilted[x, y - 2] : 0;
                tilted[x, y] = fVal + t1 + t2 - t3;
            }
        }

        return tilted;
    }

    /// <summary>
    /// Retrieves the value at (x, y) from a 2D array.
    /// If (x, y) is out of bounds, returns 0.
    /// </summary>
    private static long GetValue(long[,] integral, int x, int y)
    {
        int width = integral.GetLength(0);
        int height = integral.GetLength(1);
        if (x < 0 || y < 0 || x >= width || y >= height)
            return 0;
        return integral[x, y];
    }

    /// <summary>
    /// Computes the sum of pixel values in a rectangle defined by (x, y, width, height)
    /// using the standard integral image.
    /// </summary>
    public static long SumRectangle(long[,] integral, int x, int y, int width, int height)
    {
        int x2 = x + width - 1;
        int y2 = y + height - 1;
        long A = (x > 0 && y > 0) ? integral[x - 1, y - 1] : 0;
        long B = (y > 0) ? integral[x2, y - 1] : 0;
        long C = (x > 0) ? integral[x - 1, y2] : 0;
        long D = integral[x2, y2];
        return D - B - C + A;
    }

    /// <summary>
    /// Computes the sum of pixel values in a tilted (rotated by 45°) rectangle using the rotated integral image.
    /// 
    /// The tilted rectangle is defined by:
    ///   - (x, y): the coordinates of its top corner,
    ///   - width: the length along the tilted x–axis,
    ///   - height: the length along the tilted y–axis.
    ///
    /// The sum is computed using:
    /// 
    ///   sum = Tₜ(p) + Tₜ(s) - Tₜ(q) - Tₜ(r)
    ///
    /// where:
    ///   p = (x, y)
    ///   q = (x + width, y + width)
    ///   r = (x - height, y + height)
    ///   s = (x + width - height, y + width + height)
    /// </summary>
    public static long SumRotatedRectangle(long[,] rotatedIntegral, int x, int y, int width, int height)
    {
        long p = GetValue(rotatedIntegral, x, y);
        long q = GetValue(rotatedIntegral, x + width, y + width);
        long r = GetValue(rotatedIntegral, x - height, y + height);
        long s = GetValue(rotatedIntegral, x + width - height, y + width + height);
        return p + s - q - r;
    }
}
    



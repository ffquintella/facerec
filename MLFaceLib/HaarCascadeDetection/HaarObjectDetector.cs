using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;


namespace  MLFaceLib.HaarCascadeDetection
{
    public class HaarObjectDetector
    {
        private HaarCascadeClassifier _cascade;

        /// <summary>
        /// Creates a detector given a loaded Haar cascade.
        /// </summary>
        public HaarObjectDetector(HaarCascadeClassifier cascade)
        {
            _cascade = cascade;
        }

        /// <summary>
        /// Detects objects (faces) in the given ImageSharp image.
        /// </summary>
        /// <param name="image">Input color image.</param>
        /// <param name="scaleFactor">Scale multiplier between successive detection windows.</param>
        /// <returns>A list of rectangles where faces were detected.</returns>
        public List<SixLabors.ImageSharp.Rectangle> DetectObjects(Image image, double scaleFactor = 1.1)
        {
            List<SixLabors.ImageSharp.Rectangle> detections = new List<SixLabors.ImageSharp.Rectangle>();

            // Convert to grayscale (L8)
            using Image<L8> grayImage = image.CloneAs<L8>();

            // Compute the integral image.
            long[,] integralImage = ComputeIntegralImage(grayImage);

            // Start with the cascade’s base size.
            int baseWidth = _cascade.BaseWidth;
            int baseHeight = _cascade.BaseHeight;
            double currentScale = 1.0;
            int windowWidth = baseWidth;
            int windowHeight = baseHeight;

            // Slide a window across the image at multiple scales.
            while (windowWidth <= image.Width && windowHeight <= image.Height)
            {
                int step = Math.Max(2, windowWidth / 10); // adjust step size for performance

                for (int y = 0; y <= image.Height - windowHeight - step ; y += step)
                {
                    for (int x = 0; x <= image.Width - windowWidth - step; x += step)
                    {
                        if (PassesCascade(integralImage, x, y, windowWidth, windowHeight, baseWidth, baseHeight))
                        {
                            detections.Add(new SixLabors.ImageSharp.Rectangle(x, y, windowWidth, windowHeight));
                        }
                    }
                }

                currentScale *= scaleFactor;
                windowWidth = (int)(baseWidth * currentScale);
                windowHeight = (int)(baseHeight * currentScale);
            }

            return detections;
        }
        

        /// <summary>
        /// Applies the cascade to the detection window defined by (x, y, windowWidth, windowHeight).
        /// Returns true if the cascade accepts the window.
        /// </summary>
        private bool PassesCascade(long[,] integralImage, int windowX, int windowY, int windowWidth, int windowHeight, int baseWidth, int baseHeight)
        {
            // For each stage, compute the sum of weak classifier votes.
            foreach (var stage in _cascade.Stages)
            {
                double stageSum = 0;
                foreach (var weak in stage.WeakClassifiers)
                {
                    double vote = weak.Evaluate(integralImage, windowX, windowY, windowWidth, windowHeight, baseWidth, baseHeight);
                    stageSum += vote;
                }
                if (stageSum < stage.StageThreshold)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Computes the integral image for the grayscale image.
        /// Uses ImageSharp’s DangerousGetPixelRowMemory to access pixel data.
        /// </summary>
        private long[,] ComputeIntegralImage(Image<L8> grayImage)
        {
            int width = grayImage.Width;
            int height = grayImage.Height;
            long[,] integral = new long[width, height];

            for (int y = 0; y < height; y++)
            {
                long rowSum = 0;
                // Get the row’s pixel memory and obtain a span.
                var rowMemory = grayImage.Frames.RootFrame.DangerousGetPixelRowMemory(y);
                var rowSpan = rowMemory.Span;
                for (int x = 0; x < width; x++)
                {
                    byte pixelVal = rowSpan[x].PackedValue;
                    rowSum += pixelVal;
                    if (y == 0)
                        integral[x, y] = rowSum;
                    else
                        integral[x, y] = integral[x, y - 1] + rowSum;
                }
            }

            return integral;
        }
    }
}

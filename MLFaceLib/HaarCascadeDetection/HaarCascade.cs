using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MLFaceLib.HaarCascadeDetection
{
    /// <summary>
    /// Represents the complete Haar cascade.
    /// </summary>
    public class HaarCascade
    {
        public int BaseWidth { get; set; }
        public int BaseHeight { get; set; }
        public List<HaarStage> Stages { get; set; } = new List<HaarStage>();

        /// <summary>
        /// Loads a Haar cascade from an OpenCV XML file.
        /// </summary>
        public static HaarCascade Load(string xmlPath)
        {
            if (!File.Exists(xmlPath))
                throw new FileNotFoundException("Cascade XML file not found", xmlPath);

            XDocument doc = XDocument.Load(xmlPath);

            return Load(doc);
        }


        /// <summary>
        /// Loads a Haar cascade from an OpenCV XML file.
        /// </summary>
        public static HaarCascade LoadEmbeded(string embededResourceName)
        {
            XDocument doc = XmlResourceLoader.LoadXmlFromResource(embededResourceName);

            return Load(doc);
        }


        /// <summary>
        /// Loads a Haar cascade from an OpenCV XML file.
        /// </summary>
        public static HaarCascade Load(XDocument doc)
        {
            // Assume the cascade is stored under: <opencv_storage><cascade> ... </cascade>
            XElement cascadeElem = doc.Root.Element("cascade");
            if (cascadeElem == null)
                throw new Exception("Invalid cascade XML format: missing <cascade> element.");

            HaarCascade cascade = new HaarCascade();

            // Parse the base window size (for example: "24 24")
            var sizeElem = cascadeElem.Element("size");
            
            if (sizeElem != null)
            {
                var sizeParts = sizeElem.Value.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (sizeParts.Length == 2 &&
                    int.TryParse(sizeParts[0], out int w) &&
                    int.TryParse(sizeParts[1], out int h))
                {
                    cascade.BaseWidth = w;
                    cascade.BaseHeight = h;
                }
                else
                {
                    // default to 24x24
                    cascade.BaseWidth = 24;
                    cascade.BaseHeight = 24;
                }
            }
            else
            {
                cascade.BaseWidth = 24;
                cascade.BaseHeight = 24;
            }

            // Parse stages.
            // Depending on the cascade file, stages may be under <stages> or in a different structure.
            var stagesElem = cascadeElem.Element("stages");
            if (stagesElem == null)
                throw new Exception("Invalid cascade XML format: missing <stages> element.");
            
            var dashElements = stagesElem.Elements("_");

            // Each stage is inside an anonymous element (named "_" typically).
            foreach (var stageElem in dashElements)
            {
                HaarStage stage = new HaarStage();
                // Stage threshold
                stage.StageThreshold = double.Parse(stageElem.Element("stageThreshold").Value, CultureInfo.InvariantCulture);

                // Parse trees (each tree corresponds to one weak classifier).
                // In many cascades, a stage may contain a single tree; sometimes it contains multiple.
                var treesElem = stageElem.Element("trees");
                if (treesElem != null)
                {
                    foreach (XElement tree in treesElem.Elements("_"))
                    {
                        // In each tree, there is one (or more) weak classifier; we assume one per tree.
                        var weakElem = tree.Elements("_").FirstOrDefault();
                        if (weakElem != null)
                        {
                            HaarWeakClassifier wc = new HaarWeakClassifier();

                            // Parse the feature.
                            var featureElem = weakElem.Element("feature");
                            if (featureElem != null)
                            {
                                HaarFeature feature = new HaarFeature();
                                // The rects are stored as multiple anonymous "_" elements.
                                var rectsElem = featureElem.Element("rects");
                                if (rectsElem != null)
                                {
                                    foreach (XElement rectElem in rectsElem.Elements("_"))
                                    {
                                        // Each rect is stored as a string with five numbers: x y w h weight
                                        string[] parts = rectElem.Value.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                        if (parts.Length == 5)
                                        {
                                            int rx = int.Parse(parts[0], CultureInfo.InvariantCulture);
                                            int ry = int.Parse(parts[1], CultureInfo.InvariantCulture);
                                            int rw = int.Parse(parts[2], CultureInfo.InvariantCulture);
                                            int rh = int.Parse(parts[3], CultureInfo.InvariantCulture);
                                            double weight = double.Parse(parts[4], CultureInfo.InvariantCulture);
                                            feature.Rects.Add(new WeightedRect { X = rx, Y = ry, Width = rw, Height = rh, Weight = weight });
                                        }
                                    }
                                }
                                // Check for tilted flag.
                                XElement tiltedElem = featureElem.Element("tilted");
                                feature.Tilted = tiltedElem != null && tiltedElem.Value.Trim() == "1";
                                wc.Feature = feature;
                            }

                            // Parse weak classifier threshold and leaf values.
                            wc.Threshold = double.Parse(weakElem.Element("threshold").Value, CultureInfo.InvariantCulture);
                            // In many cascades, left_val/right_val are used (if no further node exists).
                            var leftElem = weakElem.Element("left_val");
                            var rightElem = weakElem.Element("right_val");
                            if (leftElem != null && rightElem != null)
                            {
                                wc.LeftVal = double.Parse(leftElem.Value, CultureInfo.InvariantCulture);
                                wc.RightVal = double.Parse(rightElem.Value, CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                // Alternatively, sometimes a single value is provided.
                                wc.LeftVal = 0;
                                wc.RightVal = 1;
                            }

                            stage.WeakClassifiers.Add(wc);
                        }
                    }
                }
                cascade.Stages.Add(stage);
            }

            return cascade;
        }
    }

    /// <summary>
    /// A stage in the Haar cascade.
    /// </summary>
    public class HaarStage
    {
        public double StageThreshold { get; set; }
        public List<HaarWeakClassifier> WeakClassifiers { get; set; } = new List<HaarWeakClassifier>();
    }

    /// <summary>
    /// A weak classifier (a node in the cascade tree).
    /// </summary>
    public class HaarWeakClassifier
    {
        public HaarFeature Feature { get; set; }
        public double Threshold { get; set; }
        public double LeftVal { get; set; }
        public double RightVal { get; set; }

        /// <summary>
        /// Evaluates this weak classifier on a window defined by the integral image.
        /// The parameters baseWidth/baseHeight are the cascade’s reference dimensions.
        /// </summary>
        public double Evaluate(long[,] integralImage, int windowX, int windowY, int windowWidth, int windowHeight, int baseWidth, int baseHeight)
        {
            // Compute the feature value (this sums weighted rectangle values, scaled to the window).
            double featureValue = Feature.ComputeFeature(integralImage, windowX, windowY, windowWidth, windowHeight, baseWidth, baseHeight);
            // If the feature value is less than the weak classifier threshold, use left value; otherwise, right value.
            return (featureValue < Threshold) ? LeftVal : RightVal;
        }
    }

    /// <summary>
    /// Represents a Haar feature composed of one or more weighted rectangles.
    /// </summary>
    public class HaarFeature
    {
        public List<WeightedRect> Rects { get; set; } = new List<WeightedRect>();
        /// <summary>
        /// If true, the rectangles are rotated by 45° (not implemented in this sample).
        /// </summary>
        public bool Tilted { get; set; } = false;

        /// <summary>
        /// Computes the feature value over the given window (using the integral image).
        /// The rectangles defined in the cascade are assumed to be specified for a base window (e.g., 24x24),
        /// so scaling factors are computed accordingly.
        /// </summary>
        public double ComputeFeature(long[,] integralImage, int windowX, int windowY, int windowWidth, int windowHeight, int baseWidth, int baseHeight)
        {
            double scaleX = (double)windowWidth / baseWidth;
            double scaleY = (double)windowHeight / baseHeight;
            double sum = 0;

            foreach (var rect in Rects)
            {
                // Scale rectangle coordinates
                int rx = windowX + (int)Math.Round(rect.X * scaleX);
                int ry = windowY + (int)Math.Round(rect.Y * scaleY);
                int rw = (int)Math.Round(rect.Width * scaleX);
                int rh = (int)Math.Round(rect.Height * scaleY);
                long rectSum = IntegralHelper.SumRectangle(integralImage, rx, ry, rw, rh);
                sum += rect.Weight * rectSum;
            }
            return sum;
        }
    }

    /// <summary>
    /// Represents a rectangle with an associated weight.
    /// </summary>
    public class WeightedRect
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double Weight { get; set; }
    }

    /// <summary>
    /// Helper class for integral image operations.
    /// </summary>
    public static class IntegralHelper
    {
        /// <summary>
        /// Computes the sum of pixel values in the rectangle defined by (x, y, width, height)
        /// using the provided integral image.
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
    }
}

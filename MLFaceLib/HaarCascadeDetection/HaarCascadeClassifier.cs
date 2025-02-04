using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Xml.Linq;

namespace MLFaceLib.HaarCascadeDetection
{
    /// <summary>
    /// Represents a Haar cascade classifier loaded from an OpenCV XML file.
    /// This class parses the cascade parameters, stages, weak classifiers, and Haar features.
    /// It also provides an EvaluateWindow method to compute the cascade’s decision on a detection window.
    /// </summary>
    public class HaarCascadeClassifier
    {
        public string CascadeName { get; set; }         // e.g. "haarcascade_frontalface_default"
        public string StageType { get; set; }           // e.g. "BOOST"
        public string FeatureType { get; set; }         // e.g. "HAAR"
        public int BaseWidth { get; set; }              // typically 24
        public int BaseHeight { get; set; }             // typically 24
        public List<HaarStage> Stages { get; set; } = new List<HaarStage>();

        #region Loading Methods

        /// <summary>
        /// Loads the cascade XML from the given URL (for example, the raw URL from GitHub).
        /// </summary>
        public static HaarCascadeClassifier LoadFromUrl(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                string xmlContent = client.GetStringAsync(url).Result;
                XDocument doc = XDocument.Parse(xmlContent);
                return ParseCascadeFromXDocument(doc);
            }
        }

        /// <summary>
        /// Loads the cascade XML from an embedded resource.
        /// Ensure the XML file is added to your project with its Build Action set to Embedded Resource.
        /// </summary>
        public static HaarCascadeClassifier LoadFromEmbeddedResource(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new Exception($"Resource '{resourceName}' not found. " +
                        "Verify that the file is embedded and the resource name is correct.");
                }
                XDocument doc = XDocument.Load(stream);
                return ParseCascadeFromXDocument(doc);
            }
        }

        /// <summary>
        /// Parses the cascade XML (in XDocument form) into a HaarCascadeClassifier object.
        /// This method assumes that the XML root is <opencv_storage> and that the first child element is the cascade.
        /// </summary>
        private static HaarCascadeClassifier ParseCascadeFromXDocument(XDocument doc)
        {
            // The XML file from OpenCV typically has a root element <opencv_storage> and a single child element that is the cascade.
            XElement cascadeElem = doc.Root.Elements().FirstOrDefault();
            if (cascadeElem == null)
                throw new Exception("No cascade element found in the XML.");

            HaarCascadeClassifier classifier = new HaarCascadeClassifier();
            classifier.CascadeName = cascadeElem.Name.LocalName;
            classifier.StageType = cascadeElem.Element("stageType")?.Value.Trim();
            classifier.FeatureType = cascadeElem.Element("featureType")?.Value.Trim();
            classifier.BaseWidth = int.Parse(cascadeElem.Element("width").Value.Trim(), CultureInfo.InvariantCulture);
            classifier.BaseHeight = int.Parse(cascadeElem.Element("height").Value.Trim(), CultureInfo.InvariantCulture);

            XElement stagesElem = cascadeElem.Element("stages");
            var features = cascadeElem.Element("features");
            if (stagesElem != null)
            {
                int i = 0;
                foreach (XElement stageElem in stagesElem.Elements("_"))
                {
                    HaarStage stage = new HaarStage();
                    stage.StageThreshold = double.Parse(stageElem.Element("stageThreshold").Value.Trim(), CultureInfo.InvariantCulture);

                    XElement weakClassifiersElem = stageElem.Element("weakClassifiers");
                    if (weakClassifiersElem != null)
                    {
                        foreach (XElement wcElem in weakClassifiersElem.Elements("_"))
                        {
                            HaarWeakClassifier weak = new HaarWeakClassifier();
                            
                            // Parse the internal nodes and leaf values.
                            // Typically, internalNodes is a space‑separated string, for example: "0 -1 1070.1920166015625"
                            string internalNodesStr = wcElem.Element("internalNodes").Value.Trim();
                            string leafValuesStr = wcElem.Element("leafValues").Value.Trim();
                            weak.InternalNodes = internalNodesStr
                                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
                                .ToArray();
                            weak.LeafValues = leafValuesStr
                                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
                                .ToArray();
                            
                            HaarFeature haarFeature = new HaarFeature();
                            
                            var feature = features.Elements("_").ElementAt(i);

                            i++;
                            
                            var rects = feature.Element("rects");
                            foreach (var rect in rects.Elements("_"))
                            {
                                string rectStr = rect.Value.Trim();
                                string[] parts = rectStr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length == 5)
                                {
                                    int x = int.Parse(parts[0], CultureInfo.InvariantCulture);
                                    int y = int.Parse(parts[1], CultureInfo.InvariantCulture);
                                    int w = int.Parse(parts[2], CultureInfo.InvariantCulture);
                                    int h = int.Parse(parts[3], CultureInfo.InvariantCulture);
                                    double weight = double.Parse(parts[4], CultureInfo.InvariantCulture);
                                    haarFeature.Rectangles.Add(new HaarRectangle(x, y, w, h, weight));
                                }
                            }
                            weak.Feature =  haarFeature ;
                            
                            
                            stage.WeakClassifiers.Add(weak);
                        }
                    }
                    classifier.Stages.Add(stage);
                }
            }
            return classifier;
        }

        #endregion

        /// <summary>
        /// Evaluates the cascade on a candidate detection window.
        /// The detection window is defined by its top‑left corner (windowX, windowY) and size (windowWidth, windowHeight).
        /// The method assumes that an integral image (precomputed from the input image) is provided.
        /// Returns true if the window passes all stages (i.e. a face is detected), otherwise false.
        /// </summary>
        public bool EvaluateWindow(long[,] integralImage, int windowX, int windowY, int windowWidth, int windowHeight)
        {
            foreach (var stage in Stages)
            {
                double stageSum = 0.0;
                foreach (var weak in stage.WeakClassifiers)
                {
                    double vote = weak.Evaluate(integralImage, windowX, windowY, windowWidth, windowHeight, BaseWidth, BaseHeight);
                    stageSum += vote;
                }
                if (stageSum < stage.StageThreshold)
                {
                    // The window fails this stage.
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Represents one stage of the cascade.
    /// </summary>
    public class HaarStage
    {
        public double StageThreshold { get; set; }
        public List<HaarWeakClassifier> WeakClassifiers { get; set; } = new List<HaarWeakClassifier>();
    }

    /// <summary>
    /// Represents a weak classifier (node) in the cascade.
    /// Each weak classifier contains the parsed internal nodes, leaf values, and its Haar feature.
    /// </summary>
    public class HaarWeakClassifier
    {
        public double[] InternalNodes { get; set; }
        public double[] LeafValues { get; set; }
        public HaarFeature Feature { get; set; }

        /// <summary>
        /// Evaluates the weak classifier on the detection window.
        /// The Haar feature is computed and compared with the threshold (typically the third value in InternalNodes).
        /// Returns one of the two leaf values.
        /// </summary>
        public double Evaluate(long[,] integralImage, int windowX, int windowY, int windowWidth, int windowHeight, int baseWidth, int baseHeight)
        {
            
            double featureValue = Feature.Evaluate(integralImage, windowX, windowY, windowWidth, windowHeight, baseWidth, baseHeight);
            
            // For many cascades the threshold is stored in InternalNodes[2].
            double threshold = (InternalNodes != null && InternalNodes.Length >= 3) ? InternalNodes[2] : 0;
            
            return (featureValue < threshold) ? LeafValues[0] : LeafValues[1];
        }
    }

    /// <summary>
    /// Represents a Haar feature composed of one or more weighted rectangles.
    /// </summary>
    public class HaarFeature
    {
        public List<HaarRectangle> Rectangles { get; set; } = new List<HaarRectangle>();
        public bool Tilted { get; set; } = false;

        /// <summary>
        /// Evaluates the Haar feature on a detection window.
        /// Each rectangle’s coordinates are scaled from the base window to the detection window,
        /// and the weighted sums (computed via the integral image) are accumulated.
        /// </summary>
        public double Evaluate(long[,] integralImage, int windowX, int windowY, int windowWidth, int windowHeight, int baseWidth, int baseHeight)
        {
            double scaleX = (double)windowWidth / baseWidth;
            double scaleY = (double)windowHeight / baseHeight;
            double sum = 0;
            foreach (var rect in Rectangles)
            {
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
    /// Represents a rectangle used in a Haar feature along with its weight.
    /// </summary>
    public class HaarRectangle
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double Weight { get; set; }

        public HaarRectangle(int x, int y, int width, int height, double weight)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Weight = weight;
        }
    }

    /// <summary>
    /// Helper class for computing sums over an integral image.
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

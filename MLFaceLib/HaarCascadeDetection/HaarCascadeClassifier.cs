using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Xml.Linq;

namespace MLFaceLib.HaarCascadeDetection;



/// <summary>
/// Represents a Haar cascade classifier loaded from an OpenCV XML file.
/// This class parses the cascade parameters, a global list of Haar features,
/// and the cascade stages (with weak classifiers that reference features by index).
/// </summary>
public class HaarCascadeClassifier
{
    public string CascadeName { get; set; }         // e.g. "haarcascade_frontalface_default"
    public string StageType { get; set; }           // e.g. "BOOST"
    public string FeatureType { get; set; }         // e.g. "HAAR"
    public int BaseWidth { get; set; }              // typically 24
    public int BaseHeight { get; set; }             // typically 24

    /// <summary>
    /// The global list of features parsed from the XML.
    /// </summary>
    public List<HaarFeature> Features { get; set; } = new List<HaarFeature>();

    /// <summary>
    /// The cascade stages.
    /// </summary>
    public List<HaarStage> Stages { get; set; } = new List<HaarStage>();

    #region Loading Methods

    /// <summary>
    /// Loads the cascade XML from the specified URL (e.g. the raw URL from GitHub).
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
                throw new Exception($"Resource '{resourceName}' not found. Verify the file is embedded and the resource name is correct.");
            }
            XDocument doc = XDocument.Load(stream);
            return ParseCascadeFromXDocument(doc);
        }
    }

    /// <summary>
    /// Parses the cascade XML (provided as an XDocument) into a HaarCascadeClassifier object.
    /// Assumes that the XML root is <opencv_storage> and that its first child element is the cascade.
    /// </summary>
    private static HaarCascadeClassifier ParseCascadeFromXDocument(XDocument doc)
    {
        // Typically the XML root is <opencv_storage> and the cascade is the first child element.
        XElement cascadeElem = doc.Root.Elements().FirstOrDefault();
        if (cascadeElem == null)
            throw new Exception("No cascade element found in the XML.");

        HaarCascadeClassifier classifier = new HaarCascadeClassifier();
        classifier.CascadeName = cascadeElem.Name.LocalName;
        classifier.StageType = cascadeElem.Element("stageType")?.Value.Trim();
        classifier.FeatureType = cascadeElem.Element("featureType")?.Value.Trim();
        classifier.BaseWidth = int.Parse(cascadeElem.Element("width").Value.Trim(), CultureInfo.InvariantCulture);
        classifier.BaseHeight = int.Parse(cascadeElem.Element("height").Value.Trim(), CultureInfo.InvariantCulture);

        // Parse global features.
        XElement featuresElem = cascadeElem.Element("features");
        if (featuresElem != null)
        {
            foreach (XElement featElem in featuresElem.Elements("_"))
            {
                HaarFeature feature = new HaarFeature();
                XElement rectsElem = featElem.Element("rects");
                if (rectsElem != null)
                {
                    foreach (XElement rectElem in rectsElem.Elements("_"))
                    {
                        string rectStr = rectElem.Value.Trim();
                        string[] parts = rectStr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 5)
                        {
                            int x = int.Parse(parts[0], CultureInfo.InvariantCulture);
                            int y = int.Parse(parts[1], CultureInfo.InvariantCulture);
                            int w = int.Parse(parts[2], CultureInfo.InvariantCulture);
                            int h = int.Parse(parts[3], CultureInfo.InvariantCulture);
                            double weight = double.Parse(parts[4], CultureInfo.InvariantCulture);
                            feature.Rectangles.Add(new HaarRectangle(x, y, w, h, weight));
                        }
                    }
                }
                int tilted = 0;
                if (int.TryParse(featElem.Element("tilted")?.Value.Trim(), out tilted))
                {
                    feature.Tilted = (tilted == 1);
                }
                classifier.Features.Add(feature);
            }
        }

        // Parse stages.
        XElement stagesElem = cascadeElem.Element("stages");
        if (stagesElem != null)
        {
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

                        // Parse the internal node.
                        // Expected order in internalNodes: 
                        // index 0: leftValue, index 1: rightValue, index 2: featureIndex, index 3: threshold.
                        string internalNodesStr = wcElem.Element("internalNodes").Value.Trim();
                        string[] parts = internalNodesStr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 4)
                        {
                            weak.Node = new InternalNode
                            {
                                LeftValue = int.Parse(parts[0], CultureInfo.InvariantCulture),
                                RightValue = int.Parse(parts[1], CultureInfo.InvariantCulture),
                                FeatureIndex = int.Parse(parts[2], CultureInfo.InvariantCulture),
                                Threshold = double.Parse(parts[3], CultureInfo.InvariantCulture)
                            };
                        }
                        else
                        {
                            throw new Exception("Invalid internalNodes format: expected at least 4 values.");
                        }

                        // (Note: In this refactored XML structure the feature element is not nested under weak classifiers.)
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
    /// Evaluates a candidate detection window.
    /// The window is defined by its top‑left corner (windowX, windowY) and size (windowWidth, windowHeight).
    /// Both the standard integral image and the rotated integral image (for tilted features) are provided.
    /// Returns true if the window passes all cascade stages (i.e. a face is detected); otherwise, false.
    /// </summary>
    public bool EvaluateWindow(long[,] integralImage, long[,] rotatedIntegralImage,
                               int windowX, int windowY, int windowWidth, int windowHeight)
    {
        foreach (var stage in Stages)
        {
            double stageSum = 0.0;
            foreach (var weak in stage.WeakClassifiers)
            {
                // Pass the global features list into the evaluation.
                double vote = weak.Evaluate(integralImage, rotatedIntegralImage,
                                            windowX, windowY, windowWidth, windowHeight,
                                            BaseWidth, BaseHeight, Features);
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

    /// <summary>
    /// Overloaded EvaluateWindow method that disregards the rotated integral image.
    /// It passes the standard integral image for both parameters.
    /// </summary>
    public bool EvaluateWindow(long[,] integralImage,
                               int windowX, int windowY, int windowWidth, int windowHeight)
    {
        return EvaluateWindow(integralImage, integralImage, windowX, windowY, windowWidth, windowHeight);
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
/// Each weak classifier contains its parsed InternalNode.
/// During evaluation it retrieves its associated Haar feature via the global features list.
/// </summary>
public class HaarWeakClassifier
{
    public InternalNode Node { get; set; }

    /// <summary>
    /// Evaluates this weak classifier on a candidate detection window.
    /// It retrieves the Haar feature using the Node.FeatureIndex from the provided global features list,
    /// computes the feature value, and then returns the left value if the feature value is below the threshold;
    /// otherwise, it returns the right value.
    /// </summary>
    public double Evaluate(long[,] integralImage, long[,] rotatedIntegralImage,
                           int windowX, int windowY, int windowWidth, int windowHeight,
                           int baseWidth, int baseHeight,
                           List<HaarFeature> features)
    {
        // Retrieve the Haar feature from the global features list using the feature index.
        HaarFeature feature = features[Node.FeatureIndex];
        double featureValue = feature.Evaluate(integralImage, rotatedIntegralImage,
                                               windowX, windowY, windowWidth, windowHeight,
                                               baseWidth, baseHeight);
        return (featureValue < Node.Threshold) ? Node.LeftValue : Node.RightValue;
    }
}

/// <summary>
/// Represents an internal node for a weak classifier.
/// The values are parsed from the XML in the following order:
/// index 0: leftValue, index 1: rightValue, index 2: featureIndex, index 3: threshold.
/// </summary>
public class InternalNode
{
    public double Threshold { get; set; }
    public int LeftValue { get; set; }
    public int RightValue { get; set; }
    public int FeatureIndex { get; set; }
}

/// <summary>
/// Represents a Haar feature composed of one or more weighted rectangles.
/// If Tilted is true, the feature is evaluated using the rotated integral image.
/// </summary>
public class HaarFeature
{
    public List<HaarRectangle> Rectangles { get; set; } = new List<HaarRectangle>();
    public bool Tilted { get; set; } = false;

    /// <summary>
    /// Evaluates the Haar feature on a candidate detection window.
    /// Each rectangle’s coordinates are scaled from the base window to the current window.
    /// For non-tilted features the standard integral image is used; for tilted features the rotated integral image is used.
    /// </summary>
    public double Evaluate(long[,] integralImage, long[,] rotatedIntegralImage,
                           int windowX, int windowY, int windowWidth, int windowHeight,
                           int baseWidth, int baseHeight)
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

            long rectSum = !Tilted
                ? IntegralHelper.SumRectangle(integralImage, rx, ry, rw, rh)
                : IntegralHelper.SumRotatedRectangle(rotatedIntegralImage, rx, ry, rw, rh);
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



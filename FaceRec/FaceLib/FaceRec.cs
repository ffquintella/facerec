using System.ComponentModel;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Face;
using Emgu.CV.Structure;

namespace FaceLib;

public class FaceRec
{
    private double distance = 1E+19;
    private CascadeClassifier CascadeClassifier = new CascadeClassifier(Environment.CurrentDirectory + "/Haarcascade/haarcascade_frontalface_alt.xml");
    //private Image<Bgr, byte> Frame = (Image<Bgr, byte>) null;

    private Mat mat = new Mat();
    //private List<Image<Gray, byte>> trainedFaces = new List<Image<Gray, byte>>();
    private List<Mat> trainedFaces = new List<Mat>();
    private List<int> PersonLabs = new List<int>();
    //private bool isEnable_SaveImage = false;
    //private string ImageName;
    private string setPersonName;
    public bool isTrained = false;
    private List<string> Names = new List<string>();
    private EigenFaceRecognizer eigenFaceRecognizer;
    private IContainer components = (IContainer) null;
    
    public FaceRec()
    {
        this.InitializeComponent();
        if (Directory.Exists(Environment.CurrentDirectory + "\\Image"))
            return;
        Directory.CreateDirectory(Environment.CurrentDirectory + "\\Image");
    }
    
    private void InitializeComponent()
    {
        this.eigenFaceRecognizer = new EigenFaceRecognizer(80, double.PositiveInfinity);
    }
    
    public Rectangle[] DetectFace(byte[] imageBytes)
    {
        //Bitmap bitmap = new Bitmap((Stream) new MemoryStream(image));
        
        
        //Image<Bgr, byte> resultImage = new Image<Bgr, byte>(width, height);
        //resultImage.Bytes = imageStream.ToArray();
        
        Mat mat = new Mat();
        
        CvInvoke.Imdecode(imageBytes, ImreadModes.Grayscale, mat);
        
        
        //resultImage.Bytes = image.sa;
        
        
        
        //CvInvoke.CvtColor((IInputArray) this.Frame, (IOutputArray) mat, ColorConversion.Bgr2Gray);
        CvInvoke.EqualizeHist((IInputArray) mat, (IOutputArray) mat);
        Rectangle[] rectangleArray = this.CascadeClassifier.DetectMultiScale((IInputArray) mat, 1.1, 4, new Size(), new Size());

        return rectangleArray;

    }

    
    public void SaveFace(byte[] imageBytes, Rectangle face, string imageName)
    {
        
        Mat mat = new Mat();
        CvInvoke.Imdecode(imageBytes, ImreadModes.Grayscale, mat);
        
        Image<Gray, byte> image = mat.ToImage<Gray, byte>();
        
        image.ROI = face;
        image.Resize(100, 100, Inter.Cubic).Save(Environment.CurrentDirectory + "\\Image\\" + imageName + ".jpg");

        LoadTrainedFaces();
    }
    
    private void LoadTrainedFaces()
    {
        try
        {
            int numComponents = 0;
            this.trainedFaces.Clear();
            this.PersonLabs.Clear();
            this.Names.Clear();
            foreach (string file in Directory.GetFiles(Directory.GetCurrentDirectory() + "\\Image", "*.jpg", SearchOption.AllDirectories))
            {
                this.trainedFaces.Add( new Image<Gray, byte>(file).Mat);
                this.PersonLabs.Add(numComponents);
                this.Names.Add(file);
                ++numComponents;
            }
            this.eigenFaceRecognizer = new EigenFaceRecognizer(numComponents, this.distance);
            this.eigenFaceRecognizer.Train(this.trainedFaces.ToArray(), this.PersonLabs.ToArray());
        }
        catch
        {
            throw new Exception("Error Training Faces");
        }
    }
    
    
}
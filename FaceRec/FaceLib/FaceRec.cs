using System.ComponentModel;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Face;
using Emgu.CV.Structure;

namespace FaceLib;

public class FaceRec
{
    private double distance = 1E+19;
    private CascadeClassifier CascadeClassifier = new CascadeClassifier(Environment.CurrentDirectory + "/Haarcascade/haarcascade_frontalface_alt.xml");
    private Image<Bgr, byte> Frame = (Image<Bgr, byte>) null;
//private Emgu.CV.Capture camera;
    private Mat mat = new Mat();
    private List<Image<Gray, byte>> trainedFaces = new List<Image<Gray, byte>>();
    private List<int> PersonLabs = new List<int>();
    private bool isEnable_SaveImage = false;
    private string ImageName;
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
    
}
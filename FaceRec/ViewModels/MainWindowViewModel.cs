namespace FaceRec.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string VideoRecognition  { get; } = "Sample Video Recognition APP";
    
    public MainWindowViewModel()
    {
    }

    public void EnableCamera()
    {
        // Enable Camera
    }
    
}
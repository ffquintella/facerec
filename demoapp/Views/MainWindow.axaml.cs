using Avalonia.Controls;
using demoapp.ViewModels;

namespace demoapp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        this.Opened += async (_, __) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                await vm.OnLoaded();
            }
        };
    }
    

}
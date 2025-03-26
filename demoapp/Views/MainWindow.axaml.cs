using System;
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
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Dispose().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }

}
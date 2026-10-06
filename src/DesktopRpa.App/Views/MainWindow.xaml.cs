using System.Windows;
using DesktopRpa.App.ViewModels;

namespace DesktopRpa.App.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

using Avalonia.Controls;
using Avalonia.Input;
using P1.ViewModels;

namespace P1.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    private void Image_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount != 2) 
            return;
        
        (DataContext as MainWindowViewModel)?.SideMenuResizeCommand.Execute(null);
    }

}
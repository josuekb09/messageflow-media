using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace MessageFlow.App;

public partial class SplashWindow : Window
{
    private bool closeAllowed;

    public SplashWindow()
    {
        InitializeComponent();
    }

    /// <summary>Closes the splash. The operator cannot close it by hand while startup runs.</summary>
    public void CloseSplash()
    {
        if (closeAllowed)
        {
            return;
        }

        closeAllowed = true;
        Close();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        e.Cancel = !closeAllowed;
    }
}

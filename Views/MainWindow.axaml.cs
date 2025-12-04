using Avalonia.Controls;
using Avalonia.Input;
//using Avalonia.Point;
using StockChartApp.ViewModels;

namespace StockChartApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    private void ChartArea_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && sender is Control chartControl)
        {
            // Get the position relative to the chart control
            var position = e.GetPosition(chartControl);
            
            // Update the crosshair coordinates in the ViewModel
            vm.CrosshairX = position.X;
            vm.CrosshairY = position.Y;

            // Optional: Get chart data (Time/Price) based on position here
            // This usually requires calling a HitTest/MapPixelToValue function 
            // on your StockChartControl.
        }
    }

    private void ChartArea_PointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            // Move the crosshair off-screen when the cursor leaves
            vm.CrosshairX = -100;
            vm.CrosshairY = -100;
        }
    }


    private void SymbolTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                if (vm.LoadSymbolCommand.CanExecute(null))
                    vm.LoadSymbolCommand.Execute(null);
            }
        }
    }
}
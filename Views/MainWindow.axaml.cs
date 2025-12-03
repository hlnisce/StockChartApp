using Avalonia.Controls;
using Avalonia.Input;
using StockChartApp.ViewModels;

namespace StockChartApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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
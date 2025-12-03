namespace StockChartApp.Models;

public class LabelItem
{
    public string Text { get; set; } = string.Empty;
    // Fraction (0..1) position along the axis (X: left->right, Y: top->bottom)
    public double Fraction { get; set; }
}

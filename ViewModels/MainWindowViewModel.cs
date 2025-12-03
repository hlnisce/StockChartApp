using System;
using System.Linq;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockChartApp.Models;
// LiveCharts removed — using custom chart control for candlesticks.

namespace StockChartApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string[] symbols = new[] { "SPY","QQQ","NVDA", "GOOGL", "AAPL","MSFT","META","AMZN","TSLA" };

    [ObservableProperty]
    private string selectedSymbol = string.Empty;

    [ObservableProperty]
    private double currentPrice;
    // ⭐ COMPUTED PROPERTY (No [ObservableProperty] attribute) ⭐
    public string SymbolPriceDisplay => $"{SelectedSymbol}: {CurrentPrice:F2}";
    [ObservableProperty]
    private List<OhlcPoint> candles = new List<OhlcPoint>();

    // (Removed manual X/Y label collections — LiveCharts draws axes now)

    [ObservableProperty]
    private List<LabelItem> yLabels = new List<LabelItem>();

    [ObservableProperty]
    private List<LabelItem> xLabels = new List<LabelItem>();

    [ObservableProperty]
    private string newSymbol = string.Empty;

    [ObservableProperty]
    private string[] intervals = new[] { "1m", "2m", "3m", "5m", "15m", "30m", "1h" };

    [ObservableProperty]
    private string selectedInterval = "3m";

    [RelayCommand]
    private void LoadSymbol()
    {
        if (string.IsNullOrWhiteSpace(NewSymbol))
            return;

        SelectedSymbol = NewSymbol.Trim().ToUpperInvariant();
        NewSymbol = string.Empty;
    }


    public MainWindowViewModel()
    {
        SelectedSymbol = Symbols.FirstOrDefault() ?? string.Empty;
        // Ensure interval default is applied before initial load
        SelectedInterval = SelectedInterval ?? "3m";
        if (!string.IsNullOrEmpty(SelectedSymbol))
            LoadData(SelectedSymbol);
    }

    partial void OnSelectedSymbolChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
            LoadData(value);
    }

    partial void OnSelectedIntervalChanged(string value)
    {
        // reload with new granularity
        if (!string.IsNullOrEmpty(SelectedSymbol))
            LoadData(SelectedSymbol);
    }

    private void LoadData(string symbol)
    {
        // Generate candles covering the last 12 hours using the selected interval.
        var now = DateTime.UtcNow;
        int totalMinutes = 12 * 60;
        int intervalMinutes = ParseIntervalMinutes(SelectedInterval);
        if (intervalMinutes <= 0) intervalMinutes = 3;

        int count = Math.Max(1, totalMinutes / intervalMinutes);
        var start = now.AddMinutes(-totalMinutes);

        var rnd = new Random(symbol.GetHashCode() & 0x7FFFFFFF);
        double basePrice = 100 + (rnd.NextDouble() * 400);

        double prevClose = basePrice;
        var list = new List<OhlcPoint>();
        for (int i = 0; i < count; i++)
        {
            var time = start.AddMinutes(i * intervalMinutes);
            double open = prevClose;
            // scale volatility a bit with interval size
            double vol = Math.Max(0.1, intervalMinutes / 3.0);
            double high = open + rnd.NextDouble() * vol;
            double low = open - rnd.NextDouble() * vol;
            double close = low + rnd.NextDouble() * (high - low);

            list.Add(new OhlcPoint { Time = time, Open = open, High = high, Low = low, Close = close });
            prevClose = close;
        }

        Candles = list;
        // Compute Y labels (5 ticks) and X labels (every 30 minutes)
        double min = list.Min(c => c.Low);
        double max = list.Max(c => c.High);
        double range = max - min;
        int yTicks = 5;
        var ylist = new List<LabelItem>();
        for (int t = 0; t <= yTicks; t++)
        {
            double frac = (double)t / yTicks; // 0..1 top->bottom
            double price = max - frac * range;
            ylist.Add(new LabelItem { Text = price.ToString("F2"), Fraction = frac });
        }
        // Assign the computed label lists for the view to render
        YLabels = ylist;

        var xlist = new List<LabelItem>();
        var startTick = list.First().Time;
        var endTick = list.Last().Time;
        var tick = new DateTime(startTick.Year, startTick.Month, startTick.Day, startTick.Hour, (startTick.Minute / 30) * 30, 0);
        if (tick < startTick) tick = tick.AddMinutes(30);
        var totalRangeMinutes = (endTick - startTick).TotalMinutes;
        if (totalRangeMinutes <= 0) totalRangeMinutes = 1;
        while (tick <= endTick)
        {
            double minutesFromStart = (tick - startTick).TotalMinutes;
            double frac = minutesFromStart / totalRangeMinutes;
            xlist.Add(new LabelItem { Text = tick.ToLocalTime().ToString("HH:mm"), Fraction = frac });
            tick = tick.AddMinutes(30);
        }
        XLabels = xlist;
    }

    private int ParseIntervalMinutes(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 3;
        s = s.Trim().ToLowerInvariant();
        if (s.EndsWith("m") && int.TryParse(s.Substring(0, s.Length - 1), out var m)) return m;
        if (s.EndsWith("h") && int.TryParse(s.Substring(0, s.Length - 1), out var h)) return h * 60;
        if (int.TryParse(s, out var x)) return x;
        return 3;
    }
}

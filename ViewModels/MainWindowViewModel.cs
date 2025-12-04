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
    private string[] symbols = new[] { "SPY","QQQ","AMZN","AAPL","NVDA", "GOOGL", "HOOD","MSFT","META","NFLX","TSLA" };

    [ObservableProperty]
    private string selectedSymbol = string.Empty;

    [ObservableProperty]
    private double currentPrice;
    // ⭐ COMPUTED PROPERTY (No [ObservableProperty] attribute) ⭐
    public string SymbolPriceDisplay => $"{SelectedSymbol}: {CurrentPrice:F2}";
    [ObservableProperty]
    private List<OhlcPoint> candles = new List<OhlcPoint>();
// MainWindowViewModel.cs


// ⭐ NEW PROPERTIES FOR CROSSHAIR POSITION (PIXEL COORDINATES) ⭐
    [ObservableProperty]
    private double crosshairX = -100; // Initialize off-screen
    [ObservableProperty]
    private double crosshairY = -100; // Initialize off-screen

// ... rest of your code ...
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
    private string[] periods = new[] { "1D", "1W", "1M", "1Y" };

    [ObservableProperty]
    private string selectedInterval = "3m";

    
    [ObservableProperty]
    private string selectedPeriod= "1D";

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

    partial void OnSelectedPeriodChanged(string value)
    {
        // reload with new period/timeframe
        if (!string.IsNullOrEmpty(SelectedSymbol))
            LoadData(SelectedSymbol);
    }

    private void LoadData(string symbol)
    {
        // Generate candles based on selected period and interval
        var now = DateTime.UtcNow;
        int totalMinutes = 12 * 60;  // default
        int intervalMinutes = ParseIntervalMinutes(SelectedInterval);
        if (intervalMinutes <= 0) intervalMinutes = 3;

        // Override range and interval based on selected period
        var period = SelectedPeriod?.ToUpperInvariant() ?? "1D";
        if (period == "1D")
        {
            // If the user has selected a very small interval (< 15m), show only the last 2 hours
            if (intervalMinutes < 15)
            {
                totalMinutes = 2 * 60; // last 2 hours
            }
            // If interval is between 15m and 30m, show last 8 hours
            else if (intervalMinutes < 30)
            {
                totalMinutes = 8 * 60; // last 8 hours
            }
            else
            {
                totalMinutes = 24 * 60;  // last 1 day
            }
            // For 1D keep the user-selected interval (e.g., 1m, 3m, 15m, 1h)
            // Ensure intervalMinutes is not larger than the total range
            if (intervalMinutes > totalMinutes)
                intervalMinutes = totalMinutes;
        }
        else if (period == "1W")
        {
            totalMinutes = 7 * 24 * 60;  // last 7 days
            intervalMinutes = 60;        // 1 hour candles
        }
        else if (period == "1M")
        {
            totalMinutes = 30 * 24 * 60;  // last ~30 days
            intervalMinutes = 24 * 60;    // 1 day candles
        }
        else if (period == "1Y")
        {
            totalMinutes = 365 * 24 * 60;  // last year
            // For yearly view, show weekly candles (one candle per week)
            intervalMinutes = 7 * 24 * 60; // 7 days
        }

        int count = Math.Max(1, totalMinutes / intervalMinutes);
        var start = now.AddMinutes(-totalMinutes);

        // For 1D period, align start to midnight of the current day for consistent pre-market shading
        if (period == "1D")
        {
            var today = now.Date;  // midnight of today
            start = today;
        }

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
            xlist.Add(new LabelItem { Text = tick.ToLocalTime().ToString("HHmm"), Fraction = frac });
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

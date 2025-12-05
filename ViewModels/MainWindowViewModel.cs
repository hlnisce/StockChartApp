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
    private List<ZigzagPoint> zigzagPoints = new List<ZigzagPoint>();
        
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
    int totalMinutes = 12 * 60;  // default (720 minutes)
    int intervalMinutes = ParseIntervalMinutes(SelectedInterval);
    if (intervalMinutes <= 0) intervalMinutes = 3;

    // Override range and interval based on selected period
    var period = SelectedPeriod?.ToUpperInvariant() ?? "1D";
    
    // Flag to indicate if the data range is limited (e.g., 2h or 8h)
    bool isLimitedIntradayRange = false; 

    if (period == "1D")
    {
        // 1. If the user has selected a small interval (< 15m), show only the last 2 hours
        if (intervalMinutes < 15)
        {
            totalMinutes = 2 * 60; // last 2 hours (120 minutes)
            isLimitedIntradayRange = true; // Set flag
        }
        // 2. If interval is 15m or higher (up to 1h / 60m), show the last 8 trading hours
        // This is the requirement: 8 hours inclusive of the current hour.
        else if (intervalMinutes <= 60) 
        {
            totalMinutes = 8 * 60; // last 8 hours (480 minutes)
            isLimitedIntradayRange = true; // Set flag
        }
        // 3. Else (interval > 1h, such as 2h, 4h, etc.): show the last 24 hours
        else
        {
            totalMinutes = 24 * 60;  // last 1 day (1440 minutes)
        }
        
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

    // ⭐ CRITICAL FIX: Only reset 'start' to midnight if NOT using a limited intraday range.
    if (period == "1D")
    {
        if (!isLimitedIntradayRange)
        {
            var today = now.Date;  // midnight of today
            start = today;
        }
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


// ⭐ Call the Zigzag calculation here ⭐
    CalculateZigzag(Candles); 

// ... (rest of the label calculation logic) ...

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

private double GetZigzagThreshold(string period)
{
    // The previous thresholds are kept for 1W, 1M, 1Y as they are now acceptable.

    if (period == "1D") 
    {
        // ⭐ FOCUSING ON 1m and 2m: INCREASING THRESHOLD TO REDUCE SENSITIVITY ⭐
        if (SelectedInterval is "1m" or "2m") 
        {
            // Increased from 0.0001 (0.01%) to 0.0010 (0.10%)
            return 0.0023; // 0.10% for 1m/2m charts (Less sensitive)
        }
        else if (SelectedInterval is "3m" or "5m" or "15m") 
        {
            return 0.0020; // 0.2% (Keeping this stable for medium intraday)
        }
        else 
        {
            return 0.010; // 1.0% (Keeping this stable for 1D period)
        }
    }
    
    // Keeping thresholds stable for longer periods:
    return period.ToUpperInvariant() switch
    {
        "1W" => 0.10, // 3.0%
        "1M" => 0.060, // 6.0%
        "1Y" => 0.100, // 10.0%
        _ => 0.010,
    };
}
    private void CalculateZigzag(IList<OhlcPoint> candles)
    {
        if (candles == null || candles.Count < 2)
        {
            ZigzagPoints = new List<ZigzagPoint>();
            return;
        }

        var threshold = GetZigzagThreshold(SelectedPeriod ?? "1D");
        var points = new List<ZigzagPoint>();
        
        // We start searching for the first defined swing.
        // 0: Undefined, 1: Seeking High, -1: Seeking Low
        int seekDirection = 0; 
        
        // Use the first close price as the initial turning point
        double pivotPrice = candles[0].Close;
        DateTime pivotTime = candles[0].Time;

        for (int i = 1; i < candles.Count; i++)
        {
            var currentCandle = candles[i];
            
            // Calculate the minimum change required from the last pivot
            double thresholdPrice = pivotPrice * threshold;

            if (seekDirection == 0)
            {
                // Determine initial direction: did we rise or fall enough from the first close?
                if (currentCandle.High - pivotPrice >= thresholdPrice)
                {
                    // Price rose significantly, the initial pivot was a low, now we seek a high.
                    points.Add(new ZigzagPoint { Time = pivotTime, Price = pivotPrice, IsHigh = false });
                    pivotPrice = currentCandle.High;
                    pivotTime = currentCandle.Time;
                    seekDirection = 1; // Start seeking high
                }
                else if (pivotPrice - currentCandle.Low >= thresholdPrice)
                {
                    // Price fell significantly, the initial pivot was a high, now we seek a low.
                    points.Add(new ZigzagPoint { Time = pivotTime, Price = pivotPrice, IsHigh = true });
                    pivotPrice = currentCandle.Low;
                    pivotTime = currentCandle.Time;
                    seekDirection = -1; // Start seeking low
                }
            }
            else if (seekDirection == 1) // Currently seeking High
            {
                if (currentCandle.High > pivotPrice)
                {
                    // Extend the current rising swing
                    pivotPrice = currentCandle.High;
                    pivotTime = currentCandle.Time;
                }
                else if (pivotPrice - currentCandle.Low >= thresholdPrice)
                {
                    // Price dropped significantly: the peak is found.
                    points.Add(new ZigzagPoint { Time = pivotTime, Price = pivotPrice, IsHigh = true });
                    pivotPrice = currentCandle.Low;
                    pivotTime = currentCandle.Time;
                    seekDirection = -1; // Switch to seeking low
                }
            }
            else // seekDirection == -1 (Currently seeking Low)
            {
                if (currentCandle.Low < pivotPrice)
                {
                    // Extend the current falling swing
                    pivotPrice = currentCandle.Low;
                    pivotTime = currentCandle.Time;
                }
                else if (currentCandle.High - pivotPrice >= thresholdPrice)
                {
                    // Price rose significantly: the trough is found.
                    points.Add(new ZigzagPoint { Time = pivotTime, Price = pivotPrice, IsHigh = false });
                    pivotPrice = currentCandle.High;
                    pivotTime = currentCandle.Time;
                    seekDirection = 1; // Switch to seeking high
                }
            }
        }

        // Add the final point if the list is non-empty
        if (!points.Any() && candles.Count >= 2)
        {
            // If no swings were detected, just plot the start and end to test the drawing logic itself.
            points.Add(new ZigzagPoint { Time = candles.First().Time, Price = candles.First().Close, IsHigh = true });
            points.Add(new ZigzagPoint { Time = candles.Last().Time, Price = candles.Last().Close, IsHigh = false });
        }
    // ... rest of the main for loop ...

        // --- FINAL CLEANUP LOGIC ---
        
        // Check if we have calculated any points AND if the current pivot (the last price being tracked)
        // is different from the very last point already added to the list.
        if (points.Any())
        {
            var finalCandle = candles.Last();
            var lastPoint = points[^1];
            
            // If the calculated end time/price is different from the last point added, 
            // OR if the very last candle is significantly different (to catch a quick jump), 
            // we force the final candle's time/price to be the end of the line.

            if (lastPoint.Time != finalCandle.Time)
            {
                // If the last point added wasn't the last candle, add the final candle's price/time.
                // Use the last candle's close price for the final end point.
                points.Add(new ZigzagPoint 
                { 
                    Time = finalCandle.Time, 
                    Price = finalCandle.Close, 
                    // We don't know the final direction, so IsHigh is arbitrary here.
                    IsHigh = lastPoint.IsHigh 
                });
            }
        }
        // If we never found a pivot (e.g., threshold was too high), ensure we have a start and end point.
        else if (candles.Count >= 2)
        {
            points.Add(new ZigzagPoint { Time = candles.First().Time, Price = candles.First().Close, IsHigh = true });
            points.Add(new ZigzagPoint { Time = candles.Last().Time, Price = candles.Last().Close, IsHigh = false });
        }



        ZigzagPoints = points;
    }
}

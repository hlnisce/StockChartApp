using Avalonia;
using System;
using Avalonia.Controls;
using Avalonia.Media;
using System.Collections.Generic;
using StockChartApp.Models;
using System.Linq;
using System.Globalization;
using Avalonia.Media.TextFormatting;
using StockChartApp.Patterns;


namespace StockChartApp.Views;

public class StockChartControl : Control
{

// Define a backing field for the Direct Property
// Inside StockChartControl.cs

// Define the private backing field
// Inside StockChartControl.cs

// ⭐ CRITICAL FIX: Initialize the backing field to a non-null, empty list. ⭐
private IList<PatternResult>? _detectedPatterns = new List<PatternResult>(); 

// ... rest of the DirectProperty definition ...

// Inside StockChartControl.cs: REMOVE the backing field and the RegisterDirect call

// Use standard StyledProperty registration (which should be fixed to IList<T>):
    public static readonly StyledProperty<IList<PatternResult>?> DetectedPatternsProperty =
        AvaloniaProperty.Register<StockChartControl, IList<PatternResult>?>(nameof(DetectedPatterns));

    public IList<PatternResult>? DetectedPatterns
    {
        get => GetValue(DetectedPatternsProperty);
        set => SetValue(DetectedPatternsProperty, value); // <-- This is the correct setter for StyledProperty
    }

// ... rest of the class ...
    public static readonly StyledProperty<IList<OhlcPoint>?> CandlesProperty =
        AvaloniaProperty.Register<StockChartControl, IList<OhlcPoint>?>(nameof(Candles));

    public IList<OhlcPoint>? Candles
    {
        get => GetValue(CandlesProperty);
        set => SetValue(CandlesProperty, value);
    }

    public static readonly StyledProperty<IList<PixelLabel>?> PixelYLabelsProperty =
        AvaloniaProperty.Register<StockChartControl, IList<PixelLabel>?>(nameof(PixelYLabels));

    public IList<PixelLabel>? PixelYLabels
    {
        get => GetValue(PixelYLabelsProperty);
        set => SetValue(PixelYLabelsProperty, value);
    }

    public static readonly StyledProperty<IList<PixelLabel>?> PixelXLabelsProperty =
        AvaloniaProperty.Register<StockChartControl, IList<PixelLabel>?>(nameof(PixelXLabels));

    public IList<PixelLabel>? PixelXLabels
    {
        get => GetValue(PixelXLabelsProperty);
        set => SetValue(PixelXLabelsProperty, value);
    }

    public static readonly StyledProperty<string?> PeriodProperty =
        AvaloniaProperty.Register<StockChartControl, string?>(nameof(Period), "1D");

    public string? Period
    {
        get => GetValue(PeriodProperty);
        set => SetValue(PeriodProperty, value);
    }

    public static readonly StyledProperty<List<ZigzagPoint>?> ZigzagPointsProperty =
        AvaloniaProperty.Register<StockChartControl, List<ZigzagPoint>?>(nameof(ZigzagPoints));

    public List<ZigzagPoint>? ZigzagPoints
    {
        get => GetValue(ZigzagPointsProperty);
        set => SetValue(ZigzagPointsProperty, value);
    }

    static StockChartControl()
    {
        // Make sure ZigzagPointsProperty is included in AffectsRender
        // Assuming your existing AffectsRender line looks something like this:
        AffectsRender<StockChartControl>(CandlesProperty, PeriodProperty, ZigzagPointsProperty, DetectedPatternsProperty); 
    }

public override void Render(DrawingContext context)
{
    base.Render(context);

    var rect = new Rect(Bounds.Size);
    
    // ⭐ FINAL SHADING FIX: Initialize the entire view to WHITE (Market Hours color) ⭐
    // We will overlay the Gray (Off-Hours) shade later.
    var marketHoursFillBrush = new SolidColorBrush(Color.FromArgb(100, 200, 200, 200)); 
    var offHoursFillBrush    = Brushes.White;
    
    // 1. Fill the entire chart area with WHITE (the color for market hours)
    context.FillRectangle(marketHoursFillBrush, rect);

    if (Candles == null || Candles.Count == 0)
        return;

    double leftMargin = 60; // space for price labels
    double bottomMargin = 28; // space for time labels
    double topMargin = 8;
    double rightMargin = 8;

    double width = Math.Max(1, rect.Width - leftMargin - rightMargin);
    double height = Math.Max(1, rect.Height - topMargin - bottomMargin);

    var data = Candles.ToList();
    var period = Period?.ToUpperInvariant() ?? "1D";

    // --- DRAW GRAY SHADING FOR OFF-MARKET HOURS ---
    if (period == "1D" && data.Any())
    {
        var dataStart = data.First().Time;
        var dataEnd = data.Last().Time;
        double dataTotalMinutes = (dataEnd - dataStart).TotalMinutes;
        
        if (dataTotalMinutes > 0)
        {
            var referenceDayStart = dataStart.Date; 
            
            // Market Hours: 9:30 AM to 4:00 PM (16:00)
            var marketOpen = referenceDayStart.AddHours(9).AddMinutes(30); 
            var marketClose = referenceDayStart.AddHours(16); 

            // --- 1. Shade Pre-Market (Draw GRAY over the WHITE background) ---
            if (dataStart < marketOpen)
            {
                var shadeEnd = marketOpen < dataEnd ? marketOpen : dataEnd;
                double shadeStartX = leftMargin;
                double shadeEndMinutesFromDataStart = (shadeEnd - dataStart).TotalMinutes;
                double shadeWidth = (shadeEndMinutesFromDataStart / dataTotalMinutes) * width;
                shadeWidth = Math.Max(0, Math.Min(width, shadeWidth)); 

                context.FillRectangle(offHoursFillBrush, 
                    new Rect(shadeStartX, topMargin, shadeWidth, height));
            }
            
            // --- 2. Shade Post-Market (Draw GRAY over the WHITE background) ---
            if (dataEnd > marketClose)
            {
                // Determine the actual start of the shading block. It's the later of marketClose or dataStart.
                var shadeStart = marketClose > dataStart ? marketClose : dataStart;

                // Calculate the starting pixel X coordinate
                double shadeStartMinutesFromDataStart = (shadeStart - dataStart).TotalMinutes;
                double shadeX = leftMargin + (shadeStartMinutesFromDataStart / dataTotalMinutes) * width;
                
                // Calculate total range in minutes from the shade start time to the end of data
                double totalMinutesToShade = (dataEnd - shadeStart).TotalMinutes;
                double shadeWidth = (totalMinutesToShade / dataTotalMinutes) * width;
                
                // Clamp X and Width to ensure it starts within bounds
                shadeX = Math.Max(leftMargin, shadeX);
                shadeWidth = Math.Max(0, width - (shadeX - leftMargin)); 

                context.FillRectangle(offHoursFillBrush, 
                    new Rect(shadeX, topMargin, shadeWidth, height));
            }
            
            // --- 3. If the entire visible range is off-hours, shade the whole chart GRAY. ---
            if (dataStart >= marketClose || dataEnd <= marketOpen)
            {
                context.FillRectangle(offHoursFillBrush, new Rect(leftMargin, topMargin, width, height));
            }
        }
    }
    // END OF SHADING LOGIC

    // 1. Calculate Price Range
    double min = data.Min(c => c.Low);
    double max = data.Max(c => c.High);
    double range = max - min;
    if (range <= 0) range = 1;

    int n = data.Count;
    double step = width / n;
    double candleWidth = Math.Max(2, step * 0.6);

    var pen = new Pen(Brushes.Black, 1);

    // 2. Draw Horizontal Gridlines and Y-axis Labels
    int yTicks = 5;
    for (int t = 0; t <= yTicks; t++)
    {
        double frac = (double)t / yTicks;
        double y = topMargin + frac * height;
        
        // Grid line
        var gridPen = new Pen(Brushes.LightGray, 1);
        context.DrawLine(gridPen, new Point(leftMargin, y), new Point(leftMargin + width, y));

        double price = max - frac * range;

        // Draw Y axis label text (Right-aligned in the left margin)
        try
        {
            var priceText = price.ToString("F2", CultureInfo.CurrentCulture);
            var tf = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
            var ft = new FormattedText(priceText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, 12, Brushes.Black);
            
            double tx = leftMargin - ft.Width - 4; 
            double ty = y - 8; 
            
            context.DrawText(ft, new Point(tx, ty));
        }
        catch { /* ignore */ }
    }

    // 3. Draw Candlesticks
    for (int i = 0; i < n; i++)
    {
        var p = data[i];
        double xCenter = leftMargin + step * i + step / 2;

        double yHigh = topMargin + (max - p.High) / range * height;
        double yLow = topMargin + (max - p.Low) / range * height;
        double yOpen = topMargin + (max - p.Open) / range * height;
        double yClose = topMargin + (max - p.Close) / range * height;

        // Draw wick
        context.DrawLine(pen, new Point(xCenter, yHigh), new Point(xCenter, yLow));

        // Draw body
        double left = xCenter - candleWidth / 2;
        double top = Math.Min(yOpen, yClose);
        double bodyHeight = Math.Max(1, Math.Abs(yClose - yOpen));

        IBrush fill = p.Close >= p.Open ? Brushes.DarkGreen : Brushes.Red;
        context.FillRectangle(fill, new Rect(left, top, candleWidth, bodyHeight));
        context.DrawRectangle(pen, new Rect(left, top, candleWidth, bodyHeight));
    }

    
    // Inside StockChartControl.cs / Render (after the candlesticks)

// Inside StockChartControl.cs / Render (after the candlesticks)

// Inside StockChartControl.cs / Render (after the candlesticks drawing loop finishes)

        // Draw Zigzag Line
        if (ZigzagPoints != null && ZigzagPoints.Count >= 2)
        {
            var zigzagPen = new Pen(Brushes.Blue, 2.0); // Line style

            Point? previousPoint = null;

            for (int k = 0; k < ZigzagPoints.Count; k++)
            {
                var zPoint = ZigzagPoints[k];
                
                // ⭐ FINAL CRITICAL FIX: Use a simple loop search to find the index (i) by matching Time. ⭐
                int i = -1;
                for (int idx = 0; idx < data.Count; idx++)
                {
                    // Compare the DateTime values directly
                    if (data[idx].Time == zPoint.Time)
                    {
                        i = idx;
                        break;
                    }
                }

                // If the index isn't found (i < 0), skip this point and continue to the next one.
                if (i < 0) continue; 
                
                // Calculate X and Y pixel coordinates for the swing point
                double xCenter = leftMargin + step * i + step / 2;
                double yPrice = topMargin + (max - zPoint.Price) / range * height;

                var currentPoint = new Point(xCenter, yPrice);

                if (previousPoint.HasValue)
                {
                    context.DrawLine(zigzagPen, previousPoint.Value, currentPoint);
                }

                previousPoint = currentPoint;
            }
        }

// Inside StockChartControl.cs / Render (after Zigzag drawing)

        // Draw Candlestick Pattern Markers
        if (DetectedPatterns != null && DetectedPatterns.Any())
        {
            var upBrush = Brushes.DarkGreen; // For Bullish patterns
            var downBrush = Brushes.Red;      // For Bearish patterns
            var markerPen = new Pen(Brushes.Black, 1);
            
            foreach (var pattern in DetectedPatterns)
            {
                // Find the index 'i' of the candle with the matching time
                int i = -1;
                for (int idx = 0; idx < data.Count; idx++)
                {
                    if (data[idx].Time == pattern.Time)
                    {
                        i = idx;
                        break;
                    }
                }

                if (i < 0) continue; 

                var candle = data[i];
                double xCenter = leftMargin + step * i + step / 2;
                
                // Find the Y coordinate of the candle's high or low
                double yCoord;
                IBrush fillBrush;

        // --- Bullish Engulfing (Upward Triangle) ---
                if (pattern.Type == PatternType.BullishEngulfing)
                {
                    yCoord = topMargin + (max - candle.Low) / range * height + 10; // 10 pixels below
                    fillBrush = upBrush; // Should be LimeGreen
                    
                    // ⭐ FIX: Use StreamGeometry to guarantee a closed, filled shape ⭐
                    var geometry = new StreamGeometry();
                    using (var context2 = geometry.Open())
                    {
                        // Define the three points of the triangle (upward direction)
                        context2.BeginFigure(new Point(xCenter, yCoord), isFilled: true);
                        context2.LineTo(new Point(xCenter + 6, yCoord + 12)); // Wider base
                        context2.LineTo(new Point(xCenter - 6, yCoord + 12));
                        context2.EndFigure(isClosed: true); // CLOSE THE TRIANGLE
                    }
                    
                    context.DrawGeometry(fillBrush, markerPen, geometry);
                } 
                // --- Bearish Engulfing (Downward Triangle) ---
                else if (pattern.Type == PatternType.BearishEngulfing)
                {
                    yCoord = topMargin + (max - candle.High) / range * height - 10; // 10 pixels above
                    fillBrush = downBrush; // Should be Crimson

                    // ⭐ FIX: Use StreamGeometry to guarantee a closed, filled shape ⭐
                    var geometry = new StreamGeometry();
                    using (var context2 = geometry.Open())
                    {
                        // Define the three points of the triangle (downward direction)
                        context2.BeginFigure(new Point(xCenter, yCoord), isFilled: true);
                        context2.LineTo(new Point(xCenter + 6, yCoord - 12)); // Wider base
                        context2.LineTo(new Point(xCenter - 6, yCoord - 12));
                        context2.EndFigure(isClosed: true); // CLOSE THE TRIANGLE
                    }

                    context.DrawGeometry(fillBrush, markerPen, geometry);
                }
            }
        }

            
    // 4. Draw X-axis Time Labels
    var start = data.First().Time;
    var end = data.Last().Time.AddMinutes(0);
    var totalMinutes = (end - start).TotalMinutes;
    if (totalMinutes <= 0) totalMinutes = 1;

    if (period == "1W")
    {
        var tick = new System.DateTime(start.Year, start.Month, start.Day, 0, 0, 0);
        if (tick < start) tick = tick.AddDays(1);
        while (tick <= end)
        {
            double minutesFromStart = (tick - start).TotalMinutes;
            double x = leftMargin + (minutesFromStart / totalMinutes) * width;
            context.DrawLine(pen, new Point(x, topMargin + height), new Point(x, topMargin + height + 4));
            try
            {
                var dayText = tick.ToLocalTime().ToString("ddd");
                var tf2 = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
                var ft2 = new FormattedText(dayText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf2, 12, Brushes.Black);
                double tx2 = x - ft2.Width / 2; 
                double ty2 = topMargin + height + 6;
                context.DrawText(ft2, new Point(tx2, ty2));
            }
            catch { /* ignore */ }
            tick = tick.AddDays(1);
        }
    }
    else if (period == "1M")
    {
        var tick = new System.DateTime(start.Year, start.Month, start.Day, 0, 0, 0);
        if (tick < start) tick = tick.AddDays(1);
        while (tick <= end)
        {
            double minutesFromStart = (tick - start).TotalMinutes;
            double x = leftMargin + (minutesFromStart / totalMinutes) * width;
            context.DrawLine(pen, new Point(x, topMargin + height), new Point(x, topMargin + height + 4));
            try
            {
                var dateText = tick.ToLocalTime().ToString("MM/dd");
                var tf2 = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
                var ft2 = new FormattedText(dateText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf2, 12, Brushes.Black);
                double tx2 = x - ft2.Width / 2; 
                double ty2 = topMargin + height + 6;
                context.DrawText(ft2, new Point(tx2, ty2));
            }
            catch { /* ignore */ }
            tick = tick.AddDays(1);
        }
    }
    else if (period == "1Y")
    {
        var tick = new DateTime(start.Year, start.Month, start.Day, 0, 0, 0);
        while (tick.DayOfWeek != DayOfWeek.Monday) tick = tick.AddDays(1);
        if (tick < start) tick = tick.AddDays(7);
        int? lastMonth = null;
        while (tick <= end)
        {
            double minutesFromStart = (tick - start).TotalMinutes;
            double x = leftMargin + (minutesFromStart / totalMinutes) * width;
            context.DrawLine(pen, new Point(x, topMargin + height), new Point(x, topMargin + height + 4));
            try
            {
                if (lastMonth == null || tick.Month != lastMonth)
                {
                    var monthText = tick.ToLocalTime().ToString("MMM");
                    var tf2 = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
                    var ft2 = new FormattedText(monthText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf2, 12, Brushes.Black);
                    double tx2 = x - ft2.Width / 2; 
                    double ty2 = topMargin + height + 6;
                    context.DrawText(ft2, new Point(tx2, ty2));
                    lastMonth = tick.Month;
                }
            }
            catch { /* ignore */ }
            tick = tick.AddDays(7);
        }
    }
    else if (period == "1D")
    {
        int labelIntervalMinutes = 30;
        if ((end - start).TotalMinutes <= 120) 
        {
            labelIntervalMinutes = 5;
        }

        var tick = new System.DateTime(start.Year, start.Month, start.Day, start.Hour, (start.Minute / labelIntervalMinutes) * labelIntervalMinutes, 0);
        if (tick < start) tick = tick.AddMinutes(labelIntervalMinutes);

        while (tick <= end)
        {
            double minutesFromStart = (tick - start).TotalMinutes;
            double x = leftMargin + (minutesFromStart / totalMinutes) * width;
            context.DrawLine(pen, new Point(x, topMargin + height), new Point(x, topMargin + height + 4));
            try
            {
                var timeText = tick.ToLocalTime().ToString("HH:mm");
                var tf2 = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
                var ft2 = new FormattedText(timeText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf2, 12, Brushes.Black);
                double tx2 = x - ft2.Width / 2; 
                double ty2 = topMargin + height + 6;
                context.DrawText(ft2, new Point(tx2, ty2));
            }
            catch { /* ignore */ }
            tick = tick.AddMinutes(labelIntervalMinutes);
        }
    }
    else
    {
        var tick = new System.DateTime(start.Year, start.Month, start.Day, start.Hour, (start.Minute / 30) * 30, 0);
        if (tick < start) tick = tick.AddMinutes(30);
        while (tick <= end)
        {
            double minutesFromStart = (tick - start).TotalMinutes;
            double x = leftMargin + (minutesFromStart / totalMinutes) * width;
            context.DrawLine(pen, new Point(x, topMargin + height), new Point(x, topMargin + height + 4));
            try
            {
                var timeText = tick.ToLocalTime().ToString("HHmm");
                var tf2 = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
                var ft2 = new FormattedText(timeText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf2, 12, Brushes.Black);
                double tx2 = x - ft2.Width / 2; 
                double ty2 = topMargin + height + 6;
                context.DrawText(ft2, new Point(tx2, ty2));
            }
            catch { /* ignore */ }
            tick = tick.AddMinutes(30);
        }
    }
}
}

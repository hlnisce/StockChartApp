using Avalonia;
using System;
using Avalonia.Controls;
using Avalonia.Media;
using System.Collections.Generic;
using StockChartApp.Models;
using System.Linq;
using System.Globalization;
using Avalonia.Media.TextFormatting;

namespace StockChartApp.Views;

public class StockChartControl : Control
{
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

    static StockChartControl()
    {
        AffectsRender<StockChartControl>(CandlesProperty, PeriodProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var rect = new Rect(Bounds.Size);
        context.FillRectangle(Brushes.White, rect);

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

        // For 1D period, shade all non-trading hours (outside 08:30-16:00), leaving only market hours white
        if (period == "1D")
        {
            var pmStart = data.First().Time;
            var pmEnd = data.Last().Time;
            double pmTotalMinutes = (pmEnd - pmStart).TotalMinutes;
            
            if (pmTotalMinutes > 0)
            {
                var marketStart = new DateTime(pmStart.Year, pmStart.Month, pmStart.Day, 8, 30, 0);
                var marketEnd = new DateTime(pmStart.Year, pmStart.Month, pmStart.Day, 16, 0, 0);
                
                // Shade before market opens (before 08:30)
                if (marketStart > pmStart)
                {
                    double beforeEndMin = (marketStart - pmStart).TotalMinutes;
                    double beforeWidth = (beforeEndMin / pmTotalMinutes) * width;
                    
                    context.FillRectangle(new SolidColorBrush(Color.FromArgb(50, 200, 200, 200)), 
                        new Rect(leftMargin, topMargin, beforeWidth, height));
                }
                
                // Shade after market closes (after 16:00)
                if (marketEnd < pmEnd)
                {
                    double afterStartMin = (marketEnd - pmStart).TotalMinutes;
                    double afterEndMin = pmTotalMinutes;
                    double afterX = leftMargin + (afterStartMin / pmTotalMinutes) * width;
                    double afterWidth = ((afterEndMin - afterStartMin) / pmTotalMinutes) * width;
                    
                    context.FillRectangle(new SolidColorBrush(Color.FromArgb(50, 200, 200, 200)), 
                        new Rect(afterX, topMargin, afterWidth, height));
                }
            }
        }

        double min = data.Min(c => c.Low);
        double max = data.Max(c => c.High);
        double range = max - min;
        if (range <= 0) range = 1;

        int n = data.Count;
        double step = width / n;
        double candleWidth = Math.Max(2, step * 0.6);

        var pen = new Pen(Brushes.Black, 1);

        // Draw horizontal gridlines and draw Y-axis labels directly
        int yTicks = 5;
        for (int t = 0; t <= yTicks; t++)
        {
            double frac = (double)t / yTicks;
            double y = topMargin + frac * height;
            // grid line
            var gridPen = new Pen(Brushes.LightGray, 1);
            context.DrawLine(gridPen, new Point(leftMargin, y), new Point(leftMargin + width, y));

            double price = max - frac * range;
            _ = price; // keep variable for clarity during debugging

            // Debug marker: small rectangle to indicate where Y label should be placed
            var debugBrush = Brushes.Black;
            double dbgSize = 4;
            context.FillRectangle(debugBrush, new Rect(leftMargin - 12 - dbgSize / 2, y - dbgSize / 2, dbgSize, dbgSize));

            // Draw Y axis label text aligned vertically to the gridline
            try
            {
                var priceText = price.ToString("F2", CultureInfo.CurrentCulture);
                var tf = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
                var ft = new FormattedText(priceText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, 12, Brushes.Black);
                double tx = leftMargin - 12 - 48; // approximate width offset
                double ty = y - 8; // approximate vertical center
                context.DrawText(ft, new Point(tx, ty));
            }
            catch
            {
                // ignore if FormattedText unavailable
            }
        }

        // Draw candlesticks
        for (int i = 0; i < n; i++)
        {
            var p = data[i];
            double xCenter = leftMargin + step * i + step / 2;

            double yHigh = topMargin + (max - p.High) / range * height;
            double yLow = topMargin + (max - p.Low) / range * height;
            double yOpen = topMargin + (max - p.Open) / range * height;
            double yClose = topMargin + (max - p.Close) / range * height;

            // draw wick
            context.DrawLine(pen, new Point(xCenter, yHigh), new Point(xCenter, yLow));

            // draw body
            double left = xCenter - candleWidth / 2;
            double top = Math.Min(yOpen, yClose);
            double bodyHeight = Math.Max(1, Math.Abs(yClose - yOpen));

            IBrush fill = p.Close >= p.Open ? Brushes.DarkGreen : Brushes.Red;
            context.FillRectangle(fill, new Rect(left, top, candleWidth, bodyHeight));
            context.DrawRectangle(pen, new Rect(left, top, candleWidth, bodyHeight));
        }

        // Draw X-axis time labels based on period
        var start = data.First().Time;
        var end = data.Last().Time.AddMinutes(0);
        var totalMinutes = (end - start).TotalMinutes;
        if (totalMinutes <= 0) totalMinutes = 1;

        if (period == "1W")
        {
            // For 1W: show day-of-week labels only at day boundaries
            var tick = new System.DateTime(start.Year, start.Month, start.Day, 0, 0, 0);
            if (tick < start) tick = tick.AddDays(1);

            while (tick <= end)
            {
                double minutesFromStart = (tick - start).TotalMinutes;
                double x = leftMargin + (minutesFromStart / totalMinutes) * width;
                // tick line
                context.DrawLine(pen, new Point(x, topMargin + height), new Point(x, topMargin + height + 4));

                // Draw day-of-week label centered under the tick
                try
                {
                    var dayText = tick.ToLocalTime().ToString("ddd");
                    var tf2 = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
                    var ft2 = new FormattedText(dayText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf2, 12, Brushes.Black);
                    double tx2 = x - 15; // approximate centering
                    double ty2 = topMargin + height + 6;
                    context.DrawText(ft2, new Point(tx2, ty2));
                }
                catch
                {
                    // ignore if FormattedText unavailable
                }

                tick = tick.AddDays(1);
            }
        }
        else if (period == "1M")
        {
            // For 1M: show mm/dd labels at day boundaries
            var tick = new System.DateTime(start.Year, start.Month, start.Day, 0, 0, 0);
            if (tick < start) tick = tick.AddDays(1);

            while (tick <= end)
            {
                double minutesFromStart = (tick - start).TotalMinutes;
                double x = leftMargin + (minutesFromStart / totalMinutes) * width;
                // tick line
                context.DrawLine(pen, new Point(x, topMargin + height), new Point(x, topMargin + height + 4));

                // Draw mm/dd label centered under the tick
                try
                {
                    var dateText = tick.ToLocalTime().ToString("MM/dd");
                    var tf2 = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
                    var ft2 = new FormattedText(dateText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf2, 12, Brushes.Black);
                    double tx2 = x - 20; // approximate centering
                    double ty2 = topMargin + height + 6;
                    context.DrawText(ft2, new Point(tx2, ty2));
                }
                catch
                {
                    // ignore if FormattedText unavailable
                }

                tick = tick.AddDays(1);
            }
        }
        else if (period == "1Y")
        {
            // For 1Y: show weekly ticks and draw month label when month changes
            // Align to start of week (Monday)
            var tick = new DateTime(start.Year, start.Month, start.Day, 0, 0, 0);
            // move to next Monday >= start
            while (tick.DayOfWeek != DayOfWeek.Monday) tick = tick.AddDays(1);
            if (tick < start) tick = tick.AddDays(7);

            int? lastMonth = null;
            while (tick <= end)
            {
                double minutesFromStart = (tick - start).TotalMinutes;
                double x = leftMargin + (minutesFromStart / totalMinutes) * width;
                // tick line
                context.DrawLine(pen, new Point(x, topMargin + height), new Point(x, topMargin + height + 4));

                // Draw month label only when month changes
                try
                {
                    if (lastMonth == null || tick.Month != lastMonth)
                    {
                        var monthText = tick.ToLocalTime().ToString("MMM");
                        var tf2 = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
                        var ft2 = new FormattedText(monthText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf2, 12, Brushes.Black);
                        double tx2 = x - 18; // approximate centering
                        double ty2 = topMargin + height + 6;
                        context.DrawText(ft2, new Point(tx2, ty2));
                        lastMonth = tick.Month;
                    }
                }
                catch
                {
                    // ignore if FormattedText unavailable
                }

                tick = tick.AddDays(7);
            }
        }
        else if (period == "1D")
        {
            // For 1D with small intervals (< 15m): show labels every 5 minutes
            // Otherwise show every 30 minutes
            int labelIntervalMinutes = 30;
            
            // Check if this is a small interval 1D (last 2 hours case)
            if ((end - start).TotalMinutes <= 120)  // 2 hours or less
            {
                labelIntervalMinutes = 5;
            }

            var tick = new System.DateTime(start.Year, start.Month, start.Day, start.Hour, (start.Minute / labelIntervalMinutes) * labelIntervalMinutes, 0);
            if (tick < start) tick = tick.AddMinutes(labelIntervalMinutes);

            while (tick <= end)
            {
                double minutesFromStart = (tick - start).TotalMinutes;
                double x = leftMargin + (minutesFromStart / totalMinutes) * width;
                // tick line
                context.DrawLine(pen, new Point(x, topMargin + height), new Point(x, topMargin + height + 4));

                // Draw X axis time label centered under the tick
                try
                {
                    var timeText = tick.ToLocalTime().ToString("HH:mm");
                    var tf2 = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
                    var ft2 = new FormattedText(timeText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf2, 12, Brushes.Black);
                    double tx2 = x - 20; // approximate centering
                    double ty2 = topMargin + height + 6;
                    context.DrawText(ft2, new Point(tx2, ty2));
                }
                catch
                {
                    // ignore if FormattedText unavailable
                }

                tick = tick.AddMinutes(labelIntervalMinutes);
            }
        }
        else
        {
            // For other periods (1Y): show hourly or daily labels
            var tick = new System.DateTime(start.Year, start.Month, start.Day, start.Hour, (start.Minute / 30) * 30, 0);
            if (tick < start) tick = tick.AddMinutes(30);

            while (tick <= end)
            {
                double minutesFromStart = (tick - start).TotalMinutes;
                double x = leftMargin + (minutesFromStart / totalMinutes) * width;
                // tick line
                context.DrawLine(pen, new Point(x, topMargin + height), new Point(x, topMargin + height + 4));

                // Draw X axis time label centered under the tick
                try
                {
                    var timeText = tick.ToLocalTime().ToString("HHmm");
                    var tf2 = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);
                    var ft2 = new FormattedText(timeText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf2, 12, Brushes.Black);
                    double tx2 = x - 20; // approximate centering
                    double ty2 = topMargin + height + 6;
                    context.DrawText(ft2, new Point(tx2, ty2));
                }
                catch
                {
                    // ignore if FormattedText unavailable
                }

                tick = tick.AddMinutes(30);
            }
        }
    }
}

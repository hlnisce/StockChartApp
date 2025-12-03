using System;
using Avalonia.Data.Converters;
using System.Globalization;
using System.Collections.Generic;

namespace StockChartApp.Converters;

/// <summary>
/// Multiplies two numeric inputs (double) and returns the product as double.
/// Used with MultiBinding to compute pixel offsets from normalized fractions.
/// </summary>
    public class MultiplyConverter : IMultiValueConverter
    {
        // Matches the layout margins used by the chart control so label positions align.
        private const double LeftMargin = 60.0;
        private const double RightMargin = 8.0;
        private const double TopMargin = 8.0;
        private const double BottomMargin = 28.0;

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Count < 2)
                return 0.0;

            // safe-guard nulls
            if (values[0] == null || values[1] == null)
                return 0.0;

            var s0 = values[0]?.ToString();
            var s1 = values[1]?.ToString();

            if (s0 == null || s1 == null)
                return 0.0;

            if (!double.TryParse(s0, out var frac))
                return 0.0;
            if (!double.TryParse(s1, out var total))
                return 0.0;

            var param = (parameter as string)?.ToUpperInvariant();

            if (param == "X")
            {
                // For X: compute position within inner width (chart width minus left/right margins).
                var inner = Math.Max(1.0, total - LeftMargin - RightMargin);
                return LeftMargin + frac * inner;
            }

            if (param == "Y")
            {
                // For Y: compute pixel as topMargin + frac * innerHeight so 0->top, 1->bottom
                var inner = Math.Max(1.0, total - TopMargin - BottomMargin);
                return TopMargin + frac * inner;
            }

            // Default: simple multiply
            return frac * total;
        }

        public IList<object?> ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

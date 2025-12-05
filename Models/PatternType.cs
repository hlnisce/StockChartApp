// File: StockChartApp.Models/PatternResult.cs (or similar)

using System;

namespace StockChartApp.Models
{
    /// <summary>
    /// Defines the type of candlestick pattern detected.
    /// </summary>
    public enum PatternType
    {
        None,
        BullishEngulfing,
        BearishEngulfing
        // Add other patterns here later (e.g., Hammer, Doji)
    }

    /// <summary>
    /// Stores the result of a detected pattern for rendering.
    /// </summary>
    public class PatternResult
    {
        public DateTime Time { get; set; } // The time of the last candle in the pattern
        public PatternType Type { get; set; }
    }
}
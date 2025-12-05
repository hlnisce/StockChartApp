// File: ICandlestickPattern.cs
using StockChartApp.Models;
using System.Collections.Generic;
using System;


namespace StockChartApp.Patterns
{
    public interface ICandlestickPattern
    {
        /// <summary>
        /// Gets the number of candles required to check the pattern (e.g., 2 for Engulfing).
        /// </summary>
        int LookbackPeriod { get; }

        /// <summary>
        /// Checks if the pattern is present ending at the specified index.
        /// </summary>
        /// <param name="candles">The full list of OHLC data.</param>
        /// <param name="index">The index of the last candle to check (Candle 2).</param>
        /// <returns>The detected PatternType (Bullish, Bearish, or None).</returns>
        PatternType Detect(IList<OhlcPoint> candles, int index);
    }


    public class EngulfingPattern : ICandlestickPattern
    {
        public int LookbackPeriod => 2; // Requires two candles (Candle 1 and Candle 2)

        public PatternType Detect(IList<OhlcPoint> candles, int index)
        {
            if (index < LookbackPeriod - 1) 
            {
                return PatternType.None;
            }

            var candle1 = candles[index - 1]; // Candle 1 (engulfed)
            var candle2 = candles[index];     // Candle 2 (engulfing)

            // Calculate body boundaries for clear comparison
            double body1Open = Math.Min(candle1.Open, candle1.Close);
            double body1Close = Math.Max(candle1.Open, candle1.Close);
            
            double body2Open = Math.Min(candle2.Open, candle2.Close);
            double body2Close = Math.Max(candle2.Open, candle2.Close);

            // --- 1. Bullish Engulfing ---
            // C1: Red (Close < Open) AND C2: Green (Close > Open)
            if (candle1.Close < candle1.Open && candle2.Close > candle2.Open)
            {
                // C2's body must engulf C1's body
                if (candle2.Open <= body1Open && candle2.Close >= body1Close)
                {
                    return PatternType.BullishEngulfing;
                }
            }

            // --- 2. Bearish Engulfing ---
            // C1: Green (Close > Open) AND C2: Red (Close < Open)
            if (candle1.Close > candle1.Open && candle2.Close < candle2.Open)
            {
                // C2's body must engulf C1's body
                if (candle2.Open >= body1Close && candle2.Close <= body1Open)
                {
                    return PatternType.BearishEngulfing;
                }
            }

            return PatternType.None;
        }
    }


}
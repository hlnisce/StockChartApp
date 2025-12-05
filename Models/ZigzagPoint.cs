// File: StockChartApp.Models/ZigzagPoint.cs (or similar)
using System;

namespace StockChartApp.Models;
public class ZigzagPoint
{
    public DateTime Time { get; set; }
    public double Price { get; set; }
    public bool IsHigh { get; set; } // True for a swing high, False for a swing low
}
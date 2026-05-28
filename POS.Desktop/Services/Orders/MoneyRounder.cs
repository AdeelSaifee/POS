using System;

namespace POS.Desktop.Services.Orders;

/// <summary>
/// Centralized money rounding helper using decimal math.
/// </summary>
public static class MoneyRounder
{
    /// <summary>
    /// Rounds the specified value to 2 decimal places using <see cref="MidpointRounding.AwayFromZero"/>.
    /// </summary>
    /// <param name="value">The decimal value to round.</param>
    /// <returns>The rounded decimal value.</returns>
    public static decimal Round(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}

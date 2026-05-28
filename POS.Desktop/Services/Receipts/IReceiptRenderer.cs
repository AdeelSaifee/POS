using System.Collections.Generic;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Services.Receipts;

/// <summary>
/// Defines the contract for rendering sales receipts in plain text format based on saved entities.
/// </summary>
public interface IReceiptRenderer
{
    /// <summary>
    /// Renders a transaction receipt to plain text using saved local database entities.
    /// </summary>
    /// <param name="order">The saved local order details.</param>
    /// <param name="lines">The saved order lines.</param>
    /// <param name="payments">The saved payment lines.</param>
    /// <param name="tenderMethodNames">A dictionary mapping tender method IDs to display names.</param>
    /// <returns>A formatted plain text receipt.</returns>
    string RenderReceipt(
        LocalOrder order,
        IReadOnlyList<LocalOrderLine> lines,
        IReadOnlyList<LocalPayment> payments,
        IReadOnlyDictionary<int, string> tenderMethodNames);
}

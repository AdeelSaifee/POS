using System;
using System.Collections.Generic;
using System.Text;
using POS.Desktop.Data.LocalEntities;

namespace POS.Desktop.Services.Receipts;

/// <summary>
/// Implements data-driven plain text receipt rendering using local database entities.
/// </summary>
public sealed class ReceiptRenderer : IReceiptRenderer
{
    /// <inheritdoc />
    public string RenderReceipt(
        LocalOrder order,
        IReadOnlyList<LocalOrderLine> lines,
        IReadOnlyList<LocalPayment> payments,
        IReadOnlyDictionary<int, string> tenderMethodNames)
    {
        if (order == null) throw new ArgumentNullException(nameof(order));
        if (lines == null) throw new ArgumentNullException(nameof(lines));
        if (payments == null) throw new ArgumentNullException(nameof(payments));
        if (tenderMethodNames == null) throw new ArgumentNullException(nameof(tenderMethodNames));

        var sb = new StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine("             ENTERPRISE POS             ");
        sb.AppendLine("========================================");
        sb.AppendLine($"Date:        {order.BusinessDate:yyyy-MM-dd}");
        sb.AppendLine($"Terminal ID: {order.TerminalId}");
        sb.AppendLine($"Cashier:     {order.CreatedBy}");
        sb.AppendLine($"Receipt:     {order.ReceiptNumber}");
        if (!string.IsNullOrWhiteSpace(order.GuestName))
        {
            sb.AppendLine($"Guest:       {order.GuestName}");
        }
        sb.AppendLine("----------------------------------------");

        foreach (var line in lines)
        {
            var qtyStr = line.Quantity.ToString("G");
            var itemLine = $"{line.ItemName} ({qtyStr} {line.UnitOfMeasureCode})";
            var netAmountStr = line.NetAmount.ToString("F2");
            
            // Format item line to align net amount to the right (40 characters wide)
            if (itemLine.Length + netAmountStr.Length + 1 <= 40)
            {
                var spaces = GetSpaces(40 - itemLine.Length - netAmountStr.Length);
                sb.AppendLine($"{itemLine}{spaces}{netAmountStr}");
            }
            else
            {
                sb.AppendLine(itemLine);
                var spaces = GetSpaces(40 - netAmountStr.Length);
                sb.AppendLine($"{spaces}{netAmountStr}");
            }
        }

        sb.AppendLine("----------------------------------------");
        
        var subtotalStr = order.SubtotalAmount.ToString("F2");
        sb.AppendLine($"Subtotal:   {GetSpaces(28 - subtotalStr.Length)}{subtotalStr}");

        var discountStr = order.DiscountAmount.ToString("F2");
        sb.AppendLine($"Discount:   {GetSpaces(28 - discountStr.Length)}{discountStr}");

        var taxStr = order.TaxAmount.ToString("F2");
        sb.AppendLine($"Tax:        {GetSpaces(28 - taxStr.Length)}{taxStr}");

        sb.AppendLine("========================================");
        var totalStr = order.TotalAmount.ToString("F2");
        sb.AppendLine($"TOTAL:      {GetSpaces(28 - totalStr.Length)}{totalStr}");
        sb.AppendLine("========================================");

        sb.AppendLine("TENDERED:");
        foreach (var payment in payments)
        {
            if (!tenderMethodNames.TryGetValue(payment.TenderMethodId, out var tenderName))
            {
                tenderName = $"Tender {payment.TenderMethodId}";
            }
            var amtStr = payment.Amount.ToString("F2");
            sb.AppendLine($"  {tenderName}:{GetSpaces(28 - tenderName.Length - amtStr.Length)}{amtStr}");
        }

        var changeStr = order.ChangeAmount.ToString("F2");
        sb.AppendLine($"Change Due: {GetSpaces(28 - changeStr.Length)}{changeStr}");
        sb.AppendLine("========================================");
        sb.AppendLine("      Thank you for your purchase!      ");
        sb.AppendLine("========================================");

        return sb.ToString();
    }

    private static string GetSpaces(int count)
    {
        return new string(' ', Math.Max(0, count));
    }
}

using System;

namespace POS.Desktop.Services.Payments;

/// <summary>
/// Represents the result of an order payment and atomic commit operation.
/// </summary>
/// <param name="Success">Indicates whether the payment was processed and the order completed successfully.</param>
/// <param name="OrderId">The unique identifier of the completed order, if successful.</param>
/// <param name="ReceiptNumber">The generated unique receipt number for the sale, if successful.</param>
/// <param name="ChangeAmount">The cash change computed and due back to the customer, if any.</param>
/// <param name="ErrorCode">A safe validation/error code, if unsuccessful.</param>
/// <param name="ErrorMessage">A safe user-facing error message, if unsuccessful.</param>
public sealed record PaymentCompletionResult(
    bool Success,
    Guid? OrderId = null,
    string? ReceiptNumber = null,
    decimal ChangeAmount = 0m,
    string? ErrorCode = null,
    string? ErrorMessage = null);

namespace POS.Desktop.Services.Payments;

/// <summary>
/// Represents a single payment tender item in the payment request.
/// </summary>
/// <param name="TenderMethodId">The ID of the tender method (e.g. Cash, Card, Wallet).</param>
/// <param name="Amount">The amount tendered for this line.</param>
/// <param name="ExternalPaymentReference">An optional external reference (e.g. card transaction ID or wallet reference).</param>
public sealed record PaymentTenderRequest(
    int TenderMethodId,
    decimal Amount,
    string? ExternalPaymentReference = null);

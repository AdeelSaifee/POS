using System;
using System.Collections.Generic;

namespace POS.Desktop.Services.Payments;

/// <summary>
/// Represents the payload required to complete and pay for an active order/cart.
/// </summary>
/// <param name="Tenders">The list of payment tenders applied.</param>
/// <param name="GuestName">Optional guest name for checkout.</param>
/// <param name="GuestPhone">Optional guest phone for checkout.</param>
/// <param name="IdempotencyKey">An optional unique key to enforce safe transaction retries.</param>
public sealed record PaymentCompletionRequest(
    IReadOnlyList<PaymentTenderRequest> Tenders,
    string? GuestName = null,
    string? GuestPhone = null,
    string? IdempotencyKey = null);

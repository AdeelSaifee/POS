using System.Threading;
using System.Threading.Tasks;

namespace POS.Desktop.Services.Payments;

/// <summary>
/// Defines the contract for recording tenders and committing order completions atomically.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Processes and validates payment tenders, computes cash change, and atomically commits the order, order lines, and payments.
    /// </summary>
    /// <param name="request">The payment completion payload.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing success status, generated order details, computed change, or safe validation error details.</returns>
    Task<PaymentCompletionResult> CompleteOrderAsync(
        PaymentCompletionRequest request,
        CancellationToken cancellationToken = default);
}

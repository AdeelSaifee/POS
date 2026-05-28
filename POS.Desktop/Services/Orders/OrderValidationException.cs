using System;

namespace POS.Desktop.Services.Orders;

/// <summary>
/// Exception representing cart/order validation failures that are safe to expose to the operator.
/// </summary>
public class OrderValidationException : Exception
{
    /// <summary>
    /// A distinct error code identifying the type of validation failure.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// A user-friendly message describing the validation failure.
    /// </summary>
    public string SafeMessage => Message;

    public OrderValidationException(string message, string errorCode = "VALIDATION_ERROR")
        : base(message)
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
    }
}

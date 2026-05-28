using System;

namespace POS.Desktop.Services.Payments;

/// <summary>
/// Exception thrown during payment validation when rules are violated.
/// </summary>
public class PaymentValidationException : Exception
{
    /// <summary>
    /// Gets a safe error code representing the type of rule violation.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets a safe user-facing message explaining the error.
    /// </summary>
    public string SafeMessage { get; }

    public PaymentValidationException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
        SafeMessage = message;
    }
}

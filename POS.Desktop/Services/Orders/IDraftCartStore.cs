using System;

namespace POS.Desktop.Services.Orders;

/// <summary>
/// Defines the contract for managing the process-lifetime in-memory draft cart state.
/// </summary>
public interface IDraftCartStore
{
    /// <summary>
    /// Retrieves the current state of the draft cart.
    /// </summary>
    CartStateDto GetState();

    /// <summary>
    /// Updates the state of the draft cart.
    /// </summary>
    void UpdateState(CartStateDto state);

    /// <summary>
    /// Atomically updates the state of the draft cart using the provided update function.
    /// </summary>
    /// <param name="updateFunc">The update function to apply.</param>
    /// <returns>The updated cart state.</returns>
    CartStateDto Update(Func<CartStateDto, CartStateDto> updateFunc);

    /// <summary>
    /// Resets the draft cart to an empty state.
    /// </summary>
    void Clear();
}

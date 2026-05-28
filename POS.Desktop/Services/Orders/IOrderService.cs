using System.Threading;
using System.Threading.Tasks;

namespace POS.Desktop.Services.Orders;

/// <summary>
/// Defines the contract for managing the in-memory draft cart/order state.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Retrieves the current state of the draft cart/order.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current cart state DTO.</returns>
    Task<CartStateDto> GetCartStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an item/variant to the cart. If the item already exists, increments its quantity.
    /// </summary>
    /// <param name="variantId">The database ID of the item variant to add.</param>
    /// <param name="quantity">The quantity to add (defaults to 1).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated cart state DTO.</returns>
    Task<CartStateDto> AddItemAsync(int variantId, int quantity = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the quantity of a specific item/variant in the cart.
    /// </summary>
    /// <param name="variantId">The database ID of the item variant to update.</param>
    /// <param name="quantity">The target quantity to set.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated cart state DTO.</returns>
    Task<CartStateDto> UpdateLineQuantityAsync(int variantId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a specific item/variant from the cart.
    /// </summary>
    /// <param name="variantId">The database ID of the item variant to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated cart state DTO.</returns>
    Task<CartStateDto> RemoveItemAsync(int variantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all items and active discounts from the cart.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated empty cart state DTO.</returns>
    Task<CartStateDto> ClearCartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a discount to the cart totals.
    /// </summary>
    /// <param name="discountType">The type of discount to apply (e.g. "pct" or "amount").</param>
    /// <param name="discountValue">The numeric value/rate of the discount.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated cart state DTO.</returns>
    Task<CartStateDto> ApplyDiscountAsync(string discountType, decimal discountValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes any active discount from the cart.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated cart state DTO.</returns>
    Task<CartStateDto> RemoveDiscountAsync(CancellationToken cancellationToken = default);
}

using System;

namespace POS.Desktop.Services.Orders;

/// <summary>
/// Thread-safe in-memory store for the process-lifetime draft cart state.
/// </summary>
public sealed class DraftCartStore : IDraftCartStore
{
    private CartStateDto _state = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public CartStateDto GetState()
    {
        lock (_lock)
        {
            return _state;
        }
    }

    /// <inheritdoc />
    public void UpdateState(CartStateDto state)
    {
        ArgumentNullException.ThrowIfNull(state);
        lock (_lock)
        {
            _state = state;
        }
    }

    /// <inheritdoc />
    public CartStateDto Update(Func<CartStateDto, CartStateDto> updateFunc)
    {
        ArgumentNullException.ThrowIfNull(updateFunc);
        lock (_lock)
        {
            var newState = updateFunc(_state);
            _state = newState ?? throw new InvalidOperationException("Update function returned a null state.");
            return _state;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            _state = new CartStateDto();
        }
    }
}

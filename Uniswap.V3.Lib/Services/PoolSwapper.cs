using Uniswap.V3.Lib.Enums;
using Uniswap.V3.Lib.Extensions;
using Uniswap.V3.Lib.Models;

namespace Uniswap.V3.Lib.Services;

public class PoolSwapper
{
    private readonly PoolSwapper_ExactIn_0To1 _swapper_ExactIn_0To1 = new(); 
    private readonly PoolSwapper_ExactIn_1To0 _swapper_ExactIn_1To0 = new();
    private readonly PoolSwapper_ExactOut_0To1 _swapper_ExactOut_0To1 = new();
    private readonly PoolSwapper_ExactOut_1To0 _swapper_ExactOut_1To0 = new();

    public SwapResponse Swap(PoolV3 pool, SwapRequest request)
    {
        if (!pool.Initialized)
            return new RejectedSwapResponse("Pool is not initialized.");

        if(!request.swapIn.IsValid || !request.swapOut.IsValid)
            return new RejectedSwapResponse("Swap request not valid.");

        var swapType = request.GetSwapTypeOrNull();

        if (swapType is null)
            return new RejectedSwapResponse("Swap type could not be determined from the request.");

        if(swapType == SwapType.ExactIn && (request.swapIn.AmountIn <= 0m || request.swapIn.AmountOutMinimum <= 0m))
            return new RejectedSwapResponse("Swap in amounts cannot be non-positive");

        if (swapType == SwapType.ExactOut && (request.swapOut.AmountOut <= 0m || request.swapOut.AmountInMaximum <= 0m))
            return new RejectedSwapResponse("Swap out amounts cannot be non-positive");

        var swapDirection = request.GetSwapDirectionOrNull(pool);

        if (swapDirection is null)
            return new RejectedSwapResponse("Swap direction could not be determined from the request.");

        if (!pool.TickStates.TryGetTickAtIndex(pool.CurrentTick.TickIndex.AlignTickToSpacing(pool.TickSpacing),
            out var currentTick))
            return new RejectedSwapResponse("Pool doesn't have any active positions.");

        return (swapType, swapDirection) switch
        {
            (SwapType.ExactIn, SwapDirection.Token0To1) => _swapper_ExactIn_0To1.Swap(pool, request, currentTick),
            (SwapType.ExactIn, SwapDirection.Token1To0) => _swapper_ExactIn_1To0.Swap(pool, request, currentTick),
            (SwapType.ExactOut, SwapDirection.Token0To1) => _swapper_ExactOut_0To1.Swap(pool, request, currentTick),
            (SwapType.ExactOut, SwapDirection.Token1To0) => _swapper_ExactOut_1To0.Swap(pool, request, currentTick),
            _ => throw new NotImplementedException()
        };
    }
}

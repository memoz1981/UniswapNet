using Uniswap.V3.Lib.Enums;
using Uniswap.V3.Lib.Extensions;
using Uniswap.V3.Lib.Models;

namespace Uniswap.V3.Lib.Services;

public class PoolSwapper
{
    public SwapResponse Swap(PoolV3 pool, SwapRequest request)
    {
        if (!pool.Initialized)
            return new RejectedSwapResponse("Pool is not initialized.");

        if(!request.swapIn.IsValid || !request.swapOut.IsValid)
            return new RejectedSwapResponse("Swap request not valid.");

        var swapType = request.GetSwapTypeOrNull();

        if (swapType is null)
            return new RejectedSwapResponse("Swap type could not be determined from the request.");

        var swapDirection = request.GetSwapDirectionOrNull(pool);

        if (swapDirection is null)
            return new RejectedSwapResponse("Swap direction could not be determined from the request."); 


    }

    private SwapResponse Swap(PoolV3 pool, SwapRequest request,
        SwapType swapType = SwapType.ExactIn, SwapDirection swapDirection = SwapDirection.Token0To1)
    {
        var amountIn = request.swapIn.AmountIn.Value;
        var amountOut = 0m;

        var currentSqrtPrice = pool.SqrtPrice;
        var currentTick = pool.CurrentTick; 


    }
}

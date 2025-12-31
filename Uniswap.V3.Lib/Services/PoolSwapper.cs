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

        if(swapType == SwapType.ExactIn && (request.swapIn.AmountIn <= 0m || request.swapIn.AmountOutMinimum <= 0m))
            return new RejectedSwapResponse("Swap in amounts cannot be non-positive");

        if (swapType == SwapType.ExactOut && (request.swapOut.AmountOut <= 0m || request.swapOut.AmountInMaximum <= 0m))
            return new RejectedSwapResponse("Swap out amounts cannot be non-positive");

        var swapDirection = request.GetSwapDirectionOrNull(pool);

        if (swapDirection is null)
            return new RejectedSwapResponse("Swap direction could not be determined from the request.");

        return (swapType, swapDirection) switch
        {
            (SwapType.ExactIn, SwapDirection.Token0To1) => SwapExactIn0To1(pool, request),
            _ => throw new NotImplementedException()
        };
    }

    private SwapResponse SwapExactIn0To1(PoolV3 pool, SwapRequest request)
    {
        if (!pool.TickStates.TryGetTickAtIndex(pool.CurrentTick.TickIndex.AlignTickToSpacing(pool.TickSpacing),
            out var currentTick))
            return new RejectedSwapResponse("Pool doesn't have any active positions.");

        var amountIn = request.swapIn.AmountIn.Value;
        var amountOut = 0m;

        var currentPrice = pool.SqrtPrice;
        var currentActiveLiquidity = pool.ActiveLiquidity;
        var feesUsed = 0m; 
        
        while (true)
        {
            if ((currentTick is null && !request.swapIn.TokenIn.IsZero(amountIn)) || currentActiveLiquidity <= 0m)
                return new RejectedSwapResponse("Not enough liquidity to process the swap");

            if (request.swapIn.TokenIn.IsZero(amountIn))
                break; 

            var prevPrice = currentTick.TickIndex.TickToSqrtPrice();

            var maxDeltaWithinTick = currentActiveLiquidity * (prevPrice.Inv() - currentPrice.Inv());

            var maxInputFromTraderWithinTick = maxDeltaWithinTick / (1 - pool.GetFeeTier());

            // full tick consumed
            if (amountIn >= maxInputFromTraderWithinTick)
            {
                amountIn -= maxInputFromTraderWithinTick;
                amountOut += currentActiveLiquidity * (currentPrice - prevPrice);

                currentPrice = prevPrice;
                currentActiveLiquidity -= currentTick.LiquidityNet; 
                currentTick = currentTick.Previous;
                feesUsed += (maxInputFromTraderWithinTick - maxDeltaWithinTick);
                continue;
            }

            // tick partially consumed
            var deltaToSwapWithinTick = amountIn * (1 - pool.GetFeeTier());
            var sqrtPriceNew = (currentPrice.Inv() + deltaToSwapWithinTick * currentActiveLiquidity.Inv()).Inv(); 

            amountOut += currentActiveLiquidity * (currentPrice - sqrtPriceNew);
            currentPrice = sqrtPriceNew;
            feesUsed += amountIn * pool.GetFeeTier();
            amountIn = 0m;
            break; 
        }

        if (amountOut < request.swapIn.AmountOutMinimum)
            return new RejectedSwapResponse($"Specified amount out could not be received: " +
                $"specified {request.swapIn.AmountOutMinimum}, achieved: {amountOut}");

        if(!request.swapIn.TokenIn.IsZero(amountIn))
            return new RejectedSwapResponse($"Specified amount out could not be spent: " +
                $"specified {request.swapIn.AmountIn}, left: {amountIn}");

        CommitValues(pool, currentActiveLiquidity, currentPrice, currentTick); 

        return new AcceptedSwapResponse(request.swapIn.AmountIn.Value - amountIn, amountOut); 
    }

    private void CommitValues(PoolV3 pool, decimal activeLiquidity, decimal sqrtPrice, Tick currentTick)
    {
        pool.ActiveLiquidity = activeLiquidity;
        pool.SqrtPrice = sqrtPrice;
        pool.CurrentTick = currentTick;
        pool.TickStates.Current = currentTick;
    }
}

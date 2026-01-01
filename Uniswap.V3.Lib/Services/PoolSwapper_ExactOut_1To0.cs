using Uniswap.V3.Lib.Extensions;
using Uniswap.V3.Lib.Models;

namespace Uniswap.V3.Lib.Services;

public class PoolSwapper_ExactOut_1To0
{
    public SwapResponse Swap(PoolV3 pool, SwapRequest request, Tick currentTick)
    {
        var amountOut = request.swapOut.AmountOut.Value;
        var amountIn = 0m;

        var currentPrice = pool.SqrtPrice;
        var currentActiveLiquidity = pool.ActiveLiquidity;
        var feesUsed = 0m;

        var feeGrowth1 = pool.FeeGrowthGlobal[1];

        var feeGrowthByTick = new Dictionary<int, (decimal token0, decimal token1)>();
        decimal deltaFee = 0m;
        decimal deltaFeeGrowth = 0m;

        while (true)
        {
            if (currentTick?.Next is null || currentActiveLiquidity <= 0m)
                break;

            if (request.swapOut.TokenOut.IsZero(amountOut))
                break;

            var nextPrice = currentTick.Next.TickIndex.TickToSqrtPrice();

            var maxOutputCurrentTick = currentActiveLiquidity * (currentPrice.Inv() - nextPrice.Inv());

            // full tick consumed
            if (amountOut >= maxOutputCurrentTick)
            {
                var amountInNet = currentActiveLiquidity * (nextPrice - currentPrice);
                var amountInGross = amountInNet / (1 - pool.GetFeeTier());
                amountIn += amountInGross;
                amountOut -= maxOutputCurrentTick;

                currentPrice = nextPrice;

                deltaFee = (amountInGross - amountInNet);
                feesUsed += deltaFee;
                deltaFeeGrowth = deltaFee / currentActiveLiquidity;
                feeGrowth1 += deltaFeeGrowth;
                
                
                currentTick = currentTick.Next;
                feeGrowthByTick[currentTick.TickIndex] = (currentTick.FeeGrowthOutside[0], feeGrowth1);
                currentActiveLiquidity += currentTick.LiquidityNet;
                continue;
            }

            // tick partially consumed
            var sqrtPriceNew = (currentPrice.Inv() - amountOut / currentActiveLiquidity).Inv();
            var netInput = currentActiveLiquidity * (sqrtPriceNew - currentPrice);
            var grossInput = netInput / (1 - pool.GetFeeTier());
            amountIn += grossInput;
            deltaFee = grossInput - netInput;
            deltaFeeGrowth = deltaFee / currentActiveLiquidity;

            amountOut = 0;
            currentPrice = sqrtPriceNew;
            feesUsed += deltaFee;
            feeGrowth1 += deltaFeeGrowth;
            break;
        }

        if (amountIn > request.swapOut.AmountInMaximum)
            return new RejectedSwapResponse($"Used more than minimum inputs " +
                $"specified {request.swapOut.AmountInMaximum}, achieved: {amountIn}");

        if (!request.swapOut.TokenOut.IsZero(amountOut))
            return new RejectedSwapResponse($"Specified amount out could not be sent: " +
                $"specified {request.swapOut.AmountInMaximum}, achieved: {amountIn}");

        CommitValues(pool, currentActiveLiquidity, currentPrice, currentTick, [pool.FeeGrowthGlobal[0], feeGrowth1],
            feeGrowthByTick);

        return new AcceptedSwapResponse(amountIn, request.swapOut.AmountOut.Value - amountOut);
    }

    private void CommitValues(PoolV3 pool, decimal activeLiquidity, decimal sqrtPrice, Tick currentTick, decimal[] deltaFeePool,
        Dictionary<int, (decimal token0, decimal token1)> deltaFeeGrowthByTick)
    {
        pool.ActiveLiquidity = activeLiquidity;
        pool.SqrtPrice = sqrtPrice;
        pool.CurrentTick = currentTick;
        pool.TickStates.Current = currentTick;

        pool.FeeGrowthGlobal[0] = deltaFeePool[0];
        pool.FeeGrowthGlobal[1] = deltaFeePool[1];

        foreach (var fee in deltaFeeGrowthByTick)
        {
            if (!pool.TickStates.TryGetTickAtIndex(fee.Key, out var tick))
                throw new InvalidOperationException("Tick couldn't be found");

            tick.FeeGrowthOutside[0] = fee.Value.token0;
            tick.FeeGrowthOutside[1] = fee.Value.token1;
        }
    }
}

using Uniswap.V3.Lib.Extensions;
using Uniswap.V3.Lib.Models;

public class PoolSwapper_ExactOut_0To1
{
    public SwapResponse Swap(PoolV3 pool, SwapRequest request, Tick currentTick)
    {
        var amountOut = request.swapOut.AmountOut.Value;
        var amountIn = 0m;

        var currentPrice = pool.SqrtPrice;
        var currentActiveLiquidity = pool.ActiveLiquidity;
        var feesUsed = 0m;

        var feeGrowth0 = pool.FeeGrowthGlobal[0];

        var feeGrowthByTick = new Dictionary<int, (decimal token0, decimal token1)>();
        decimal deltaFee = 0m;
        decimal deltaFeeGrowth = 0m;

        while (true)
        {
            if (currentTick is null || currentActiveLiquidity <= 0m)
                break;

            if (request.swapOut.TokenOut.IsZero(amountOut))
                break;

            var prevPrice = currentTick.TickIndex.TickToSqrtPrice();

            var maxOutputCurrentTick = currentActiveLiquidity * (currentPrice - prevPrice);

            // full tick consumed
            if (amountOut >= maxOutputCurrentTick)
            {
                var amountInNet = currentActiveLiquidity * (prevPrice.Inv() - currentPrice.Inv());
                var amountInGross = amountInNet / (1 - pool.GetFeeTier());
                amountIn += amountInGross;
                amountOut -= maxOutputCurrentTick;
                
                currentPrice = prevPrice;

                deltaFee = (amountInGross - amountInNet);
                feesUsed += deltaFee;
                deltaFeeGrowth = deltaFee / currentActiveLiquidity;
                feeGrowth0 += deltaFeeGrowth;
                feeGrowthByTick[currentTick.TickIndex] = (feeGrowth0, currentTick.FeeGrowthOutside[1]);
                currentActiveLiquidity -= currentTick.LiquidityNet;
                currentTick = currentTick.Previous;

                continue;
            }

            // tick partially consumed
            var sqrtPriceNew = currentPrice - amountOut / currentActiveLiquidity;
            var netInput = currentActiveLiquidity * (sqrtPriceNew.Inv() - currentPrice.Inv());
            var grossInput = netInput / (1 - pool.GetFeeTier());
            amountIn += grossInput;
            deltaFee = grossInput - netInput;
            deltaFeeGrowth = deltaFee / currentActiveLiquidity; 

            amountOut = 0;
            currentPrice = sqrtPriceNew;
            feesUsed += deltaFee;
            feeGrowth0 += deltaFeeGrowth;
            break;
        }

        if (amountIn > request.swapOut.AmountInMaximum)
            return new RejectedSwapResponse($"Used more than minimum inputs " +
                $"specified {request.swapOut.AmountInMaximum}, achieved: {amountIn}");

        if(!request.swapOut.TokenOut.IsZero(amountOut))
            return new RejectedSwapResponse($"Specified amount out could not be sent: " +
                $"specified {request.swapOut.AmountInMaximum}, achieved: {amountIn}");

        CommitValues(pool, currentActiveLiquidity, currentPrice, currentTick, [feeGrowth0, pool.FeeGrowthGlobal[1]],
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

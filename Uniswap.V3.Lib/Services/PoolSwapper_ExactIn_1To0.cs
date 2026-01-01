using Uniswap.V3.Lib.Extensions;
using Uniswap.V3.Lib.Models;

namespace Uniswap.V3.Lib.Services
{
    public class PoolSwapper_ExactIn_1To0
    {
        public SwapResponse Swap(PoolV3 pool, SwapRequest request, Tick currentTick)
        {
            var amountIn = request.swapIn.AmountIn.Value;
            var amountOut = 0m;

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

                if (request.swapIn.TokenIn.IsZero(amountIn))
                    break;

                var nextPrice = currentTick.Next.TickIndex.TickToSqrtPrice();

                var maxDeltaWithinTick = currentActiveLiquidity * (nextPrice - currentPrice);

                var maxInputFromTraderWithinTick = maxDeltaWithinTick / (1 - pool.GetFeeTier());

                // full tick consumed
                if (amountIn >= maxInputFromTraderWithinTick)
                {
                    amountIn -= maxInputFromTraderWithinTick;
                    amountOut += currentActiveLiquidity * (currentPrice.Inv() - nextPrice.Inv());

                    currentPrice = nextPrice;

                    deltaFee = (maxInputFromTraderWithinTick - maxDeltaWithinTick);
                    feesUsed += deltaFee;
                    deltaFeeGrowth = deltaFee / currentActiveLiquidity;
                    feeGrowth1 += deltaFeeGrowth;
                    currentTick = currentTick.Next;
                    currentActiveLiquidity += currentTick.LiquidityNet;
                    feeGrowthByTick[currentTick.TickIndex] = (currentTick.FeeGrowthOutside[0], feeGrowth1);
                    continue;
                }

                // tick partially consumed
                var deltaToSwapWithinTick = amountIn * (1 - pool.GetFeeTier());
                var sqrtPriceNew = currentPrice + deltaToSwapWithinTick / currentActiveLiquidity;

                amountOut += currentActiveLiquidity * (currentPrice.Inv() - sqrtPriceNew.Inv());
                currentPrice = sqrtPriceNew;
                deltaFee = amountIn * pool.GetFeeTier();
                feesUsed += deltaFee;
                deltaFeeGrowth = deltaFee / currentActiveLiquidity;
                feeGrowth1 += deltaFeeGrowth;
                amountIn = 0m;
                break;
            }

            if (amountOut < request.swapIn.AmountOutMinimum)
                return new RejectedSwapResponse($"Specified amount out could not be received: " +
                    $"specified {request.swapIn.AmountOutMinimum}, achieved: {amountOut}");

            CommitValues(pool, currentActiveLiquidity, currentPrice, currentTick, [pool.FeeGrowthGlobal[0], feeGrowth1],
                feeGrowthByTick);

            return new AcceptedSwapResponse(request.swapIn.AmountIn.Value - amountIn, amountOut);
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
}

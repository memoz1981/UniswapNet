using Uniswap.V3.Lib.Extensions;
using Uniswap.V3.Lib.Models;
using Uniswap.V3.Lib.Persistence;

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

            var priceLimit = request.swapIn.PriceLimit.Value;

            if (currentPrice >= priceLimit)
                return new RejectedSwapResponse("Current price is above or at the limit price");

            var feeGrowth1 = pool.FeeGrowthGlobal[1];

            var feeGrowthByTick = new Dictionary<int, (decimal token0, decimal token1)>();
            decimal[] protocolFees = [pool.ProtocolFees[0], pool.ProtocolFees[1]];

            while (true)
            {
                if (currentTick?.Next is null || currentActiveLiquidity <= 0m)
                    break; 

                if (request.swapIn.TokenIn.IsZero(amountIn))
                    break;

                var nextPrice = currentTick.Next.TickIndex.TickToSqrtPrice();

                var maxValuesForTick = pool.CalculateSwapStep1_0(currentPrice, nextPrice, currentActiveLiquidity); 

                // full tick consumed
                if (amountIn >= maxValuesForTick.grossInput && nextPrice <= priceLimit)
                {
                    amountIn -= maxValuesForTick.grossInput;
                    amountOut += maxValuesForTick.output;

                    currentPrice = nextPrice;
                    feesUsed += maxValuesForTick.deltaFeeLP;
                    feeGrowth1 += maxValuesForTick.deltaFeeGrowth;
                    currentTick = currentTick.Next;
                    currentActiveLiquidity += currentTick.LiquidityNet;
                    feeGrowthByTick[currentTick.TickIndex] = (currentTick.FeeGrowthOutside[0], feeGrowth1);
                    protocolFees[1] += maxValuesForTick.protocolFee;
                    continue;
                }

                // tick partially consumed
                var deltaToSwapWithinTick = amountIn * (1 - pool.GetFeeTier());
                var sqrtPriceNew = currentPrice + deltaToSwapWithinTick / currentActiveLiquidity;
                sqrtPriceNew = sqrtPriceNew <= priceLimit ? sqrtPriceNew : priceLimit;

                var valuesWithinTick = pool.CalculateSwapStep1_0(currentPrice, sqrtPriceNew, currentActiveLiquidity);
                amountOut += valuesWithinTick.output;
                currentPrice = sqrtPriceNew;
                feesUsed += valuesWithinTick.deltaFeeLP;
                feeGrowth1 += valuesWithinTick.deltaFeeGrowth;
                amountIn -= valuesWithinTick.grossInput;
                protocolFees[1] += valuesWithinTick.protocolFee;
                break;
            }

            if (amountOut < request.swapIn.AmountOutMinimum)
                return new RejectedSwapResponse($"Specified amount out could not be received: " +
                    $"specified {request.swapIn.AmountOutMinimum}, achieved: {amountOut}");

            if (request.recipient is not null)
            {
                if (!request.recipient.CanSuccessfullyReceive)
                    return new RejectedSwapResponse("Recipient cannot accept funds");

                request.recipient.Receive(request.swapOut.TokenOut, amountOut);
            }
            else
            {
                var trader = TraderRepo.Traders.FirstOrDefault(tr => tr.Id == request.traderId);

                if (!trader.CanSuccessfullyReceive)
                    return new RejectedSwapResponse("Trader cannot accept funds");
                trader.Receive(request.swapOut.TokenOut, amountOut);
            }

            CommitValues(pool, currentActiveLiquidity, currentPrice, currentTick, [pool.FeeGrowthGlobal[0], feeGrowth1],
                feeGrowthByTick, protocolFees);

            return new AcceptedSwapResponse(request.swapIn.AmountIn.Value - amountIn, amountOut);
        }

        private void CommitValues(PoolV3 pool, decimal activeLiquidity, decimal sqrtPrice, Tick currentTick, decimal[] deltaFeePool,
        Dictionary<int, (decimal token0, decimal token1)> deltaFeeGrowthByTick, decimal[] protocolFees)
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

            pool.ProtocolFees = [protocolFees[0], protocolFees[1]];
        }
    }
}

using Uniswap.V3.Lib.Extensions;
using Uniswap.V3.Lib.Models;
using Uniswap.V3.Lib.Persistence;

public class PoolSwapper_ExactOut_0To1
{
    public SwapResponse Swap(PoolV3 pool, SwapRequest request, Tick currentTick)
    {
        var amountOut = request.swapOut.AmountOut.Value;
        var amountIn = 0m;

        var currentPrice = pool.SqrtPrice;
        var currentActiveLiquidity = pool.ActiveLiquidity;
        var feesUsed = 0m;

        var priceLimit = request.swapOut.PriceLimit.Value;

        if (currentPrice <= priceLimit)
            return new RejectedSwapResponse("Current price below price limit.");

        var feeGrowth0 = pool.FeeGrowthGlobal[0];

        var feeGrowthByTick = new Dictionary<int, (decimal token0, decimal token1)>();

        while (true)
        {
            if (currentTick is null || currentActiveLiquidity <= 0m)
                break;

            if (request.swapOut.TokenOut.IsZero(amountOut))
                break;

            if (currentPrice <= priceLimit)
                break;

            var prevPrice = currentTick.TickIndex.TickToSqrtPrice();

            var maxValuesWithinTick = pool.CalculateSwapStep0_1(currentPrice, prevPrice, currentActiveLiquidity);

            // full tick consumed
            if (amountOut >= maxValuesWithinTick.output && prevPrice >= priceLimit)
            {
                amountIn += maxValuesWithinTick.grossInput;
                amountOut -= maxValuesWithinTick.output;
                
                currentPrice = prevPrice;

                feesUsed += maxValuesWithinTick.deltaFee;
                feeGrowth0 += maxValuesWithinTick.deltaFeeGrowth;
                feeGrowthByTick[currentTick.TickIndex] = (feeGrowth0, currentTick.FeeGrowthOutside[1]);
                currentActiveLiquidity -= currentTick.LiquidityNet;
                currentTick = currentTick.Previous;

                continue;
            }

            // tick partially consumed
            var sqrtPriceNew = currentPrice - amountOut / currentActiveLiquidity;
            sqrtPriceNew = sqrtPriceNew > priceLimit ? sqrtPriceNew : priceLimit;
            var amountToFinalPrice = pool.CalculateSwapStep0_1(currentPrice, sqrtPriceNew, currentActiveLiquidity);

            amountIn += amountToFinalPrice.grossInput;

            amountOut -= amountToFinalPrice.output;
            currentPrice = sqrtPriceNew;
            feesUsed += amountToFinalPrice.deltaFee;
            feeGrowth0 += amountToFinalPrice.deltaFeeGrowth;
            break;
        }

        var amountOutDelivered = request.swapOut.AmountOut.Value - amountOut;

        if (amountIn > request.swapOut.AmountInMaximum)
            return new RejectedSwapResponse($"Used more than maximum input " +
                $"specified {request.swapOut.AmountInMaximum}, used: {amountIn}");

        if (!request.swapOut.TokenOut.IsZero(amountOut))
            return new RejectedSwapResponse($"Specified amount out could not be sent: " +
                $"specified {request.swapOut.AmountOut}, achieved: {amountOutDelivered}");

        if (request.recipient is not null)
        {
            if (!request.recipient.CanSuccessfullyReceive)
                return new RejectedSwapResponse("Recipient cannot accept funds");

            request.recipient.Receive(request.swapOut.TokenOut, amountOutDelivered);
        }
        else
        {
            var trader = TraderRepo.Traders.FirstOrDefault(tr => tr.Id == request.traderId);

            if (!trader.CanSuccessfullyReceive)
                return new RejectedSwapResponse("Trader cannot accept funds");
            trader.Receive(request.swapOut.TokenOut, amountOutDelivered);
        }

        CommitValues(pool, currentActiveLiquidity, currentPrice, currentTick, [feeGrowth0, pool.FeeGrowthGlobal[1]],
            feeGrowthByTick);

        return new AcceptedSwapResponse(amountIn, amountOutDelivered);
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

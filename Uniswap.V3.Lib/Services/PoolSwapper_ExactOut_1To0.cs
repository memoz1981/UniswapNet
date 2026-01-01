using Uniswap.V3.Lib.Extensions;
using Uniswap.V3.Lib.Models;
using Uniswap.V3.Lib.Persistence;

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

        var priceLimit = request.swapOut.PriceLimit.Value;

        if (currentPrice >= priceLimit)
            return new RejectedSwapResponse("Current price is above or at the limit price");

        var feeGrowth1 = pool.FeeGrowthGlobal[1];

        var feeGrowthByTick = new Dictionary<int, (decimal token0, decimal token1)>();

        while (true)
        {
            if (currentTick?.Next is null || currentActiveLiquidity <= 0m)
                break;

            if (request.swapOut.TokenOut.IsZero(amountOut))
                break;

            var nextPrice = currentTick.Next.TickIndex.TickToSqrtPrice();

            var maxValuesForTick = pool.CalculateSwapStep1_0(currentPrice, nextPrice, currentActiveLiquidity);

            // full tick consumed
            if (amountOut >= maxValuesForTick.output && nextPrice <= priceLimit)
            {
                amountIn += maxValuesForTick.grossInput;
                amountOut -= maxValuesForTick.output;

                currentPrice = nextPrice;
                feesUsed += maxValuesForTick.deltaFee;
                feeGrowth1 += maxValuesForTick.deltaFeeGrowth;
                
                currentTick = currentTick.Next;
                feeGrowthByTick[currentTick.TickIndex] = (currentTick.FeeGrowthOutside[0], feeGrowth1);
                currentActiveLiquidity += currentTick.LiquidityNet;
                continue;
            }

            // tick partially consumed
            var sqrtPriceNew = (currentPrice.Inv() - amountOut / currentActiveLiquidity).Inv();
            sqrtPriceNew = sqrtPriceNew <= priceLimit ? sqrtPriceNew : priceLimit;
            var valuesWithinTick = pool.CalculateSwapStep1_0(currentPrice, sqrtPriceNew, currentActiveLiquidity);

            amountIn += valuesWithinTick.grossInput;
            amountOut -= valuesWithinTick.output;
            currentPrice = sqrtPriceNew;
            feesUsed += valuesWithinTick.deltaFee;
            feeGrowth1 += valuesWithinTick.deltaFeeGrowth;
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

        CommitValues(pool, currentActiveLiquidity, currentPrice, currentTick, [pool.FeeGrowthGlobal[0], feeGrowth1],
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

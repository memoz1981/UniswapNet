using System.Diagnostics;
using Uniswap.V3.Lib.Extensions;
using Uniswap.V3.Lib.Models;
using Uniswap.V3.Lib.Persistence;

namespace Uniswap.V3.Lib.Services;

public class PoolSwapper_ExactIn_0To1
{
    public SwapResponse Swap(PoolV3 pool, SwapRequest request, Tick currentTick)
    {
        var amountIn = request.swapIn.AmountIn.Value;
        var amountOut = 0m;

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

                deltaFee = (maxInputFromTraderWithinTick - maxDeltaWithinTick);
                feesUsed += deltaFee;
                deltaFeeGrowth = deltaFee / currentActiveLiquidity;
                feeGrowth0 += deltaFeeGrowth;
                feeGrowthByTick[currentTick.TickIndex] = (feeGrowth0, currentTick.FeeGrowthOutside[1]);
                currentActiveLiquidity -= currentTick.LiquidityNet;
                currentTick = currentTick.Previous;

                continue;
            }

            // tick partially consumed
            var deltaToSwapWithinTick = amountIn * (1 - pool.GetFeeTier());
            var sqrtPriceNew = (currentPrice.Inv() + deltaToSwapWithinTick * currentActiveLiquidity.Inv()).Inv();

            amountOut += currentActiveLiquidity * (currentPrice - sqrtPriceNew);
            currentPrice = sqrtPriceNew;
            deltaFee = amountIn * pool.GetFeeTier();
            feesUsed += deltaFee;
            deltaFeeGrowth = deltaFee / currentActiveLiquidity;
            feeGrowth0 += deltaFeeGrowth;
            amountIn = 0m;
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

        CommitValues(pool, currentActiveLiquidity, currentPrice, currentTick, [feeGrowth0, pool.FeeGrowthGlobal[1]],
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

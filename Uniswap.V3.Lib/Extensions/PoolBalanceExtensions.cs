using Uniswap.V3.Lib.Models;

namespace Uniswap.V3.Lib.Extensions;

public static class PoolBalanceExtensions
{
    public static decimal[] CalculateTokenBalances(this PoolV3 pool)
    {
        decimal balance0 = 0m;
        decimal balance1 = 0m;

        if (pool.TickStates.Current == null)
            return [0m, 0m];

        if(pool.TickStates.TryGetTickAtIndex(pool.TickStates.Current.TickIndex, out var current))
            return [0m, 0m];

        var currentPrice = pool.SqrtPrice;
        var currentTickIndex = pool.CurrentTick.TickIndex;

        // Traverse all positions
        foreach (var position in pool.Positions.Values)
        {
            var tickLowerPrice = position.TickLower.TickIndex.TickToSqrtPrice();
            var tickUpperPrice = position.TickUpper.TickIndex.TickToSqrtPrice();
            var liquidity = position.Liquidity;

            if (liquidity == 0m)
                continue;

            // Position entirely below current price (all token0)
            if (currentPrice <= tickLowerPrice)
            {
                balance0 += liquidity * (tickUpperPrice - tickLowerPrice)
                    / (tickUpperPrice * tickLowerPrice);
            }
            // Position entirely above current price (all token1)
            else if (currentPrice >= tickUpperPrice)
            {
                balance1 += liquidity * (tickUpperPrice - tickLowerPrice);
            }
            // Position contains current price (split)
            else
            {
                balance0 += liquidity * (tickUpperPrice - currentPrice)
                    / (tickUpperPrice * currentPrice);
                balance1 += liquidity * (currentPrice - tickLowerPrice);
            }
        }

        return [balance0, balance1];
    }

    public static decimal[] CalculateTokenBalancesAfterFlash(this PoolV3 pool,
        decimal amount0Borrowed, decimal amount1Borrowed, decimal fee0, decimal fee1)
    {
        var currentBalances = pool.CalculateTokenBalances();

        // After flash, pool should have: current - borrowed + (borrowed + fees returned)
        // Net effect: current + fees
        return [
            currentBalances[0] + fee0,
        currentBalances[1] + fee1
        ];
    }
}

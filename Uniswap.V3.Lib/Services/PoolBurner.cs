using Uniswap.V3.Lib.Enums;
using Uniswap.V3.Lib.Extensions;
using Uniswap.V3.Lib.Models;

namespace Uniswap.V3.Lib.Services;

public class PoolBurner
{
    public BurnResponse Burn(PoolV3 pool, BurnRequest request)
    {
        if(!pool.Initialized)
            return new RejectedBurnResponse("Pool is not initialized.");

        if (request.LiquidityToBurn < 0)
            return new RejectedBurnResponse("Liquidity to burn cannot be negative");

        if (!pool.Positions.TryGetValue(request.PositionId, out var position) || position.LpId != request.LpId)
            return new RejectedBurnResponse("Position doesnt exist");

        if (position.Liquidity == 0m || request.LiquidityToBurn > position.Liquidity)
            return new RejectedBurnResponse("Position doesn't have enough liquidity.");

        var feesFromThisBurn = UpdatePosition(pool, position);

        var principals = UpdatePrincipalAmounts(pool, position, request);

        decimal[] amountsToBurn = [feesFromThisBurn[0] + principals[0], feesFromThisBurn[1] + principals[1]];

        position.Liquidity -= request.LiquidityToBurn;

        position.TickLower.LiquidityGross -= request.LiquidityToBurn;
        position.TickLower.LiquidityNet -= request.LiquidityToBurn;

        position.TickUpper.LiquidityGross -= request.LiquidityToBurn;
        position.TickUpper.LiquidityNet += request.LiquidityToBurn;

        UpdateTickStates(pool, position);

        UpdatePoolActiveLiquidity(pool, position, request);

        return new AcceptedBurnResponse(position.Id, amountsToBurn, position.Liquidity); 
    }

    private decimal[] UpdatePosition(PoolV3 pool, PoolV3Position position)
    {
        var feesNow = pool.GetFeeGrowthInsideForPosition(position.TickLower, position.TickUpper);

        var deltaFee0 = feesNow[0] - position.FeeGrowthInsideLast[0];
        var deltaFee1 = feesNow[1] - position.FeeGrowthInsideLast[1];

        var feesFromThisBurn0 = deltaFee0 * position.Liquidity; 
        var feesFromThisBurn1 = deltaFee1 * position.Liquidity;

        position.TokensOwed[0] += feesFromThisBurn0;
        position.TokensOwed[1] += feesFromThisBurn1;

        position.FeeGrowthInsideLast[0] = feesNow[0]; 
        position.FeeGrowthInsideLast[1] = feesNow[1];

        return [feesFromThisBurn0, feesFromThisBurn1];
    }

    private decimal[] UpdatePrincipalAmounts(PoolV3 pool, PoolV3Position position, BurnRequest request)
    {
        if (request.LiquidityToBurn == 0m)
            return [0m, 0m];

        var currentPrice = pool.SqrtPrice; 
        var priceLower = position.TickLower.TickIndex.TickToSqrtPrice();
        var priceUpper = position.TickUpper.TickIndex.TickToSqrtPrice();

        var principal0 = currentPrice < priceLower
            ? request.LiquidityToBurn * (priceUpper - priceLower) / (priceUpper * priceLower)
            : currentPrice > priceUpper
            ? 0m : request.LiquidityToBurn * (priceUpper - currentPrice) / (priceUpper * currentPrice);

        var principal1 = currentPrice < priceLower
            ? 0m
            : currentPrice > priceUpper
            ? request.LiquidityToBurn * (priceUpper - priceLower) 
            : request.LiquidityToBurn * (currentPrice - priceLower);

        position.TokensOwed[0] += principal0; 
        position.TokensOwed[1] += principal1;

        return [principal0, principal1];
    }

    private void UpdateTickStates(PoolV3 pool, PoolV3Position position)
    {
        if (position.TickLower.LiquidityGross == 0m && position.TickLower.LiquidityNet == 0m)
            position.TickLower.DeActivate();

        if (position.TickUpper.LiquidityGross == 0m && position.TickUpper.LiquidityNet == 0m)
            position.TickUpper.DeActivate();
    }

    private void UpdatePoolActiveLiquidity(PoolV3 pool, PoolV3Position position, BurnRequest request)
    {
        if (pool.CurrentTick.TickIndex >= position.TickLower.TickIndex
            && pool.CurrentTick.TickIndex < position.TickUpper.TickIndex)
            pool.ActiveLiquidity -= request.LiquidityToBurn; 
    }
}

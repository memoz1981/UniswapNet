using Uniswap.V3.Lib.Extensions;
using Uniswap.V3.Lib.Models;

namespace Uniswap.V3.Lib.Services;

public class PoolMinter
{
    public MintResponse Mint(PoolV3 pool, MintRequest request)
    {
        if (!pool.Initialized)
            return new RejectedMintResponse("Pool is not initialized.");

        if (request.TokenAmounts is null || request.TokenAmounts.Length != 2)
            return new RejectedMintResponse("Provided token array should have 2 elements");

        if (request.TokenAmounts[0] is null && request.TokenAmounts[1] is null)
            return new RejectedMintResponse("Mint request without both token amounts.");

        if (request.PriceMax <= request.PriceMin)
            return new RejectedMintResponse("Mint request price min higher than or equal to price max.");

        var tickMin = request.PriceMin.PriceToTick().AlignTickToSpacing(pool.TickSpacing);
        var tickMax = request.PriceMax.PriceToTick().AlignTickToSpacing(pool.TickSpacing);

        if (tickMin == tickMax)
            return new RejectedMintResponse("Price range too narrow");

        var sqrtPriceLower = tickMin.TickToSqrtPrice();
        var sqrtPriceUpper = tickMax.TickToSqrtPrice(); 

        if (pool.SqrtPrice <= sqrtPriceLower)
            return MintToken0Only(pool, request, sqrtPriceLower, sqrtPriceUpper, tickMin, tickMax);

        if (sqrtPriceUpper <= pool.SqrtPrice)
            return MintToken1Only(pool, request, sqrtPriceLower, sqrtPriceUpper, tickMin, tickMax);

        return MintBothTokens(pool, request, sqrtPriceLower, sqrtPriceUpper, tickMin, tickMax); 
    }

    private MintResponse MintToken0Only(PoolV3 pool, MintRequest request, 
        decimal sqrtPriceLower, decimal sqrtPriceUpper, int tickMin, int tickMax)
    {
        if ((request.TokenAmounts[0] ?? 0) == 0)
            return new RejectedMintResponse("Token 0 should be supplied, current price is below request min price.");

        var liquidity = request.TokenAmounts[0].Value * sqrtPriceLower * sqrtPriceUpper
                / (sqrtPriceUpper - sqrtPriceLower);

        var positionId = pool.Mint(request.LpId, tickMin, tickMax, liquidity);
        
        return new AcceptedMintResponse(positionId, tickMin.TickToPrice(), tickMax.TickToPrice(),
            [request.TokenAmounts[0].Value, 0], liquidity);
    }

    private MintResponse MintToken1Only(PoolV3 pool, MintRequest request,
        decimal sqrtPriceLower, decimal sqrtPriceUpper, int tickMin, int tickMax)
    {
        if ((request.TokenAmounts[1] ?? 0) == 0)
            return new RejectedMintResponse("Token 1 should be supplied, current price is above request max price.");

        var liquidity = request.TokenAmounts[1].Value / (sqrtPriceUpper - sqrtPriceLower);

        var positionId = pool.Mint(request.LpId, tickMin, tickMax, liquidity);

        return new AcceptedMintResponse(positionId, tickMin.TickToPrice(), tickMax.TickToPrice(),
            [0, request.TokenAmounts[1].Value], liquidity);
    }

    private MintResponse MintBothTokens(PoolV3 pool, MintRequest request, 
        decimal sqrtPriceLower, decimal sqrtPriceUpper, int tickMin, int tickMax)
    {
        var liquidityToken0 = request.TokenAmounts[0] != null ? 
            request.TokenAmounts[0].Value * pool.SqrtPrice * sqrtPriceUpper 
            / (sqrtPriceUpper - pool.SqrtPrice) 
            : decimal.MaxValue;

        var liquidityToken1 = request.TokenAmounts[1] != null ? 
            request.TokenAmounts[1].Value / (pool.SqrtPrice - sqrtPriceLower) : decimal.MaxValue;

        var liquidity = Math.Min(liquidityToken0, liquidityToken1);

        var token0AmountUsed = liquidity * (sqrtPriceUpper - pool.SqrtPrice) / (pool.SqrtPrice * sqrtPriceUpper);
        var token1AmountUsed = liquidity * (pool.SqrtPrice - sqrtPriceLower);

        var positionId = pool.Mint(request.LpId, tickMin, tickMax, liquidity);

        return new AcceptedMintResponse(positionId, tickMin.TickToPrice(), tickMax.TickToPrice(),
            [token0AmountUsed, token1AmountUsed], liquidity);
    }
}

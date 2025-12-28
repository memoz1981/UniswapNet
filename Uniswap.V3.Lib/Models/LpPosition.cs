namespace Uniswap.V3.Lib.Models;

public class LpPosition 
{
    public LpPosition(int positionId, decimal priceMin, decimal priceMax, decimal[] tokensOwed, decimal liquidity)
    {
        PositionId = positionId;
        PriceMin = priceMin;
        PriceMax = priceMax;
        TokensOwed = tokensOwed;
        Liquidity = liquidity;
    }

    public int PositionId { get; set; }
    public decimal PriceMin { get; set; }
    public decimal PriceMax { get; set; }
    public decimal[] TokensOwed { get; set; }
    public decimal Liquidity { get; set; }
} 

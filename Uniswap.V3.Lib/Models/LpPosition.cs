namespace Uniswap.V3.Lib.Models; 

public record struct LpPosition(int PositionId, decimal PriceMin, decimal PriceMax,
    decimal[] TokenAmounts, decimal Liquidity);

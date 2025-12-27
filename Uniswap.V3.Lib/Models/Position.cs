namespace Uniswap.V3.Lib.Models;

public record struct Position
{
    private static int _index = 0;
    public int Id { get; init; }
    public int LpId { get; init; }
    public int TickLower { get; init; }
    public int TickUpper { get; init; }
    public decimal Liquidity { get; init; }

    public decimal[] FeeGrowthInsideLast { get; private set; }
    public decimal[] TokensOwed { get; private set; }

    public Position(int lpId, int tickLower, int tickUpper, decimal liquidity, decimal[] feeGrowthInside)
    {
        if (feeGrowthInside is null || feeGrowthInside.Length != 2)
            throw new ArgumentException("Fee growth should be array of size 2...");
        
        Id = _index++;
        LpId = lpId;
        TickLower = tickLower;
        TickUpper = tickUpper;
        Liquidity = liquidity;
        FeeGrowthInsideLast = [feeGrowthInside[0], feeGrowthInside[1]]; 
        TokensOwed = [0, 0]; 
    }
}

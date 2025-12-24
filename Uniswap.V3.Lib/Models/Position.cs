namespace Uniswap.V3.Lib.Models;

public record struct Position
{
    private static int _index = 0;
    public int Id { get; init; }
    public int OwnerId { get; init; }
    public int TickLower { get; init; }
    public int TickUpper { get; init; }
    public decimal Liquidity { get; init; }

    public decimal FeeGrowthInside0Last { get; private set; }
    public decimal FeeGrowthInside1Last { get; private set; }
    public decimal TokensOwed0 { get; private set; }
    public decimal TokensOwed1 { get; private set; }

    public Position(int ownerId, int tickLower, int tickUpper, decimal liquidity)
    {
        Id = _index++;
        OwnerId = ownerId;
        TickLower = tickLower;
        TickUpper = tickUpper;
        Liquidity = liquidity;
    }
}

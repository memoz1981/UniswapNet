namespace Uniswap.V3.Lib.Models;

public record struct Tick
{
    public Tick(int tickIndex)
    {
        TickIndex = tickIndex;
        FeeGrowthOutside = [0, 0];
        LiquidityGross = 0m;
        LiquidityNet = 0m; 
    }
    public int TickIndex { get; init; }
    public decimal LiquidityGross { get; set; }
    public decimal LiquidityNet { get; set; }
    public decimal[] FeeGrowthOutside { get; set; }
}

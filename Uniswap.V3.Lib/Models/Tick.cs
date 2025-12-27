using System;

namespace Uniswap.V3.Lib.Models;

public record struct Tick
{
    public Tick(int tickIndex)
    {
        TickIndex = tickIndex;
        LiquidityGross = 0m;
        LiquidityNet = 0m;
        FeeGrowthOutside = [0, 0];
    }

    public Tick(int tickIndex, decimal liquidityGross, decimal liquitidyNet, decimal[] feeGrowthOutside)
    {
        TickIndex = tickIndex;
        LiquidityGross = liquidityGross;
        LiquidityNet = liquitidyNet;

        if (feeGrowthOutside is null || feeGrowthOutside.Length != 2)
            throw new ArgumentException("Fee growth array cannot be null and should have length of 2"); 

        FeeGrowthOutside = [0, 0];
    }

    public int TickIndex { get; init; }
    public decimal LiquidityGross { get; set; }
    public decimal LiquidityNet { get; set; }
    public decimal[] FeeGrowthOutside { get; set; }

    public bool HasZeroFeeGrowth => FeeGrowthOutside[0] == 0m && FeeGrowthOutside[1] == 0m; 

    public Tick AddToThis(Tick other)
    {
        if (other.TickIndex != TickIndex)
            throw new ArgumentException("Adding ticks with unequal indices not allowed.");

        // we omit the added fee growth numbers as this one is the base (existing)... 
        return new Tick(TickIndex, other.LiquidityGross + LiquidityGross, other.LiquidityNet + LiquidityNet,
            FeeGrowthOutside);
    }
}

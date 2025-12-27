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

    public Tick(int tickIndex, decimal liquidityGross, decimal liquitidyNet)
    {
        TickIndex = tickIndex;
        LiquidityGross = liquidityGross;
        LiquidityNet = liquitidyNet;
        FeeGrowthOutside = [0, 0];
    }

    public int TickIndex { get; init; }
    public decimal LiquidityGross { get; set; }
    public decimal LiquidityNet { get; set; }
    public decimal[] FeeGrowthOutside { get; set; }

    public static Tick operator +(Tick a, Tick b)
    {
        if (a.TickIndex != b.TickIndex)
            throw new ArgumentException("Adding ticks with unequal indices not allowed.");

        return new Tick(a.TickIndex, a.LiquidityGross + b.LiquidityGross, a.LiquidityNet + b.LiquidityNet);
    }

    public bool Equals(Tick other)
        => TickIndex == other.TickIndex;

    public override int GetHashCode()
        => HashCode.Combine(TickIndex);
}

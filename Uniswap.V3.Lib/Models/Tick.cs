using Uniswap.V3.Lib.Enums;

namespace Uniswap.V3.Lib.Models;

public class Tick
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

        FeeGrowthOutside = [feeGrowthOutside[0], feeGrowthOutside[1]];
        State = TickState.Initialized;
        Previous = null;
        Next = null; 
    }

    public int TickIndex { get; init; }
    public decimal LiquidityGross { get; set; }
    public decimal LiquidityNet { get; set; }
    public decimal[] FeeGrowthOutside { get; set; }
    public Tick Previous { get; private set; }
    public Tick Next { get; private set; }
    public TickState State { get; private set; }
    public void Activate() => State = TickState.Initialized;
    public void DeActivate() => State = TickState.DeInitialized;
    public void SetPrevious(Tick tick) => Previous = tick; 
    public void SetNext(Tick tick) => Next = tick;

    public bool HasZeroFeeGrowth => FeeGrowthOutside[0] == 0m && FeeGrowthOutside[1] == 0m; 

    public Tick AddToThis(Tick other)
    {
        if (other.TickIndex != TickIndex)
            throw new ArgumentException("Adding ticks with unequal indices not allowed.");

        LiquidityGross += other.LiquidityGross; 
        LiquidityNet += other.LiquidityNet;
        this.Activate();

        return this;
    }
}

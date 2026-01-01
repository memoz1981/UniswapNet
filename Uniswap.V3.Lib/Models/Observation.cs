namespace Uniswap.V3.Lib.Models;

public class Observation
{
    public Observation(uint blockTimestamp, long tickCumulative,
        decimal secondsPerLiquidityCumulative, bool initialized)
    {
        BlockTimestamp = blockTimestamp;
        TickCumulative = tickCumulative;
        SecondsPerLiquidityCumulative = secondsPerLiquidityCumulative;
        Initialized = initialized;
    }

    public uint BlockTimestamp { get; set; }
    public long TickCumulative { get; set; }
    public decimal SecondsPerLiquidityCumulative { get; set; }
    public bool Initialized { get; set; }
}

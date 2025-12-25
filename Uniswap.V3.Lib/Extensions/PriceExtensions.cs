namespace Uniswap.V3.Lib.Extensions;

public static class PriceExtensions
{
    public static int PriceToTick(this decimal price)
        => (int)Math.Floor(Math.Log((double)price, 1.0001));

    public static decimal TickToPrice(this int tick)
        => (decimal)Math.Pow(1.0001, tick);

    public static int SqrtPriceToTick(this decimal sqrtPrice)
        => (int)Math.Floor(Math.Log(Math.Pow((double)sqrtPrice, 2), 1.0001));

    public static decimal TickToSqrtPrice(this int tick)
        => (decimal)Math.Pow(Math.Sqrt(1.0001), tick);

    public static int AlignTickToSpacing(this int tick, int spacing)
    {
        if (spacing <= 0)
            throw new ArgumentException("spacing should be non-zero.");

        return (int)Math.Floor((double)tick / spacing) * spacing;
    }

    public static decimal ToSqrtPrice(this decimal price) 
        => (decimal)Math.Sqrt((double)price);
}

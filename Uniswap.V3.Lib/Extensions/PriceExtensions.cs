using Uniswap.V3.Lib.Models;

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

    public static (decimal grossInput, decimal output, decimal deltaFeeLP, decimal deltaFeeGrowth, decimal protocolFee) 
        CalculateSwapStep0_1(this PoolV3 pool, decimal currentPrice, decimal prevPrice, decimal activeLiquidity)
    {
        var netInput = activeLiquidity * (prevPrice.Inv() - currentPrice.Inv());
        var grossInput = netInput / (1 - pool.GetFeeTier());
        var output = activeLiquidity * (currentPrice - prevPrice);

        var deltaFee = grossInput - netInput;
        var protocolFee = deltaFee * pool.ProtocolFee / 256m;
        var deltaFeeLP = deltaFee - protocolFee; 
        var deltaFeeGrowth = deltaFeeLP / activeLiquidity; 

        return (grossInput, output, deltaFeeLP, deltaFeeGrowth, protocolFee);
    }

    public static (decimal grossInput, decimal output, decimal deltaFeeLP, decimal deltaFeeGrowth, decimal protocolFee)
        CalculateSwapStep1_0(this PoolV3 pool, decimal currentPrice, decimal nextPrice, decimal activeLiquidity)
    {
        var netInput = activeLiquidity * (nextPrice - currentPrice);
        var grossInput = netInput / (1 - pool.GetFeeTier());
        var output = activeLiquidity * (currentPrice.Inv() - nextPrice.Inv());

        var deltaFee = grossInput - netInput;
        var protocolFee = deltaFee * pool.ProtocolFee / 256m;
        var deltaFeeLP = deltaFee - protocolFee;
        var deltaFeeGrowth = deltaFeeLP / activeLiquidity;

        return (grossInput, output, deltaFeeLP, deltaFeeGrowth, protocolFee);
    }
}

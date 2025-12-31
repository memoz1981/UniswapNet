using Uniswap.V3.Lib.Models;

namespace Uniswap.V3.Lib.Extensions;

public static class PoolExtensions
{
    public static decimal[] GetFeeGrowthInsideForPosition(this PoolV3 pool, Tick tickLower, Tick tickUpper)
    {
        decimal feeBelow0 = 0m;
        decimal feeBelow1 = 0m;

        decimal feeAbove0 = 0m;
        decimal feeAbove1 = 0m;

        // assign lower fee
        if (tickLower.TickIndex <= pool.CurrentTick.TickIndex)
        {
            feeBelow0 = tickLower.FeeGrowthOutside[0];
            feeBelow1 = tickLower.FeeGrowthOutside[1];
        }
        else
        {
            feeBelow0 = pool.FeeGrowthGlobal[0] - tickLower.FeeGrowthOutside[0];
            feeBelow1 = pool.FeeGrowthGlobal[1] - tickLower.FeeGrowthOutside[1];
        }

        //assign upper fee
        if (pool.CurrentTick.TickIndex < tickUpper.TickIndex)
        {
            feeAbove0 = tickUpper.FeeGrowthOutside[0];
            feeAbove1 = tickUpper.FeeGrowthOutside[1];
        }
        else
        {
            feeAbove0 = pool.FeeGrowthGlobal[0] - tickUpper.FeeGrowthOutside[0];
            feeAbove1 = pool.FeeGrowthGlobal[1] - tickUpper.FeeGrowthOutside[1];
        }

        var feeGrowthInside0 = pool.FeeGrowthGlobal[0] - feeBelow0 - feeAbove0;
        var feeGrowthInside1 = pool.FeeGrowthGlobal[1] - feeBelow1 - feeAbove1;

        return [feeGrowthInside0, feeGrowthInside1];
    }

    public static decimal GetFeeTier(this PoolV3 pool) => (decimal)pool.FeeTier / 10_000m; 
}

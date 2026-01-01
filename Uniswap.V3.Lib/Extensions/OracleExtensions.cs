using Uniswap.V3.Lib.Helpers;
using Uniswap.V3.Lib.Models;

namespace Uniswap.V3.Lib.Extensions
{
    public static class OracleExtensions
    {
        public static void UpdateObservation(this PoolV3 pool, uint blockTimestamp)
        {
            var last = pool.Observations[pool.ObservationIndex];

            // Don't update if same timestamp
            if (last.BlockTimestamp == blockTimestamp)
                return;

            var timeDelta = blockTimestamp - last.BlockTimestamp;

            // Calculate new cumulative values
            var tickCumulative = last.TickCumulative + (pool.CurrentTick.TickIndex * (long)timeDelta);

            var secondsPerLiquidityCumulative = last.SecondsPerLiquidityCumulative;
            if (pool.ActiveLiquidity > 0)
            {
                secondsPerLiquidityCumulative += (decimal)timeDelta / pool.ActiveLiquidity;
            }

            // Write observation
            WriteObservation(pool, blockTimestamp, tickCumulative, secondsPerLiquidityCumulative);
        }

        private static void WriteObservation(PoolV3 pool, uint blockTimestamp,
            long tickCumulative, decimal secondsPerLiquidityCumulative)
        {
            var index = pool.ObservationIndex;
            var cardinality = pool.ObservationCardinality;
            var cardinalityNext = pool.ObservationCardinalityNext;

            // Increase cardinality if needed
            if (cardinalityNext > cardinality && index == cardinality - 1)
            {
                cardinality = cardinalityNext;
                pool.ObservationCardinality = cardinality;
            }

            // Move to next index (circular buffer)
            var nextIndex = (ushort)((index + 1) % cardinality);
            pool.ObservationIndex = nextIndex;

            // Ensure array is large enough
            if (pool.Observations.Length < cardinality)
            {
                var newObservations = new Observation[cardinality];
                Array.Copy(pool.Observations, newObservations, pool.Observations.Length);
                pool.Observations = newObservations;
            }

            // Write new observation
            pool.Observations[nextIndex] = new Observation(
                blockTimestamp,
                tickCumulative,
                secondsPerLiquidityCumulative,
                initialized: true
            );
        }

        public static void IncreaseObservationCardinalityNext(this PoolV3 pool, ushort observationCardinalityNext)
        {
            if (observationCardinalityNext <= pool.ObservationCardinalityNext)
                return;

            pool.ObservationCardinalityNext = observationCardinalityNext;
        }

        public static (long tickCumulative, decimal secondsPerLiquidityCumulative) Observe(
            this PoolV3 pool, uint secondsAgo)
        {
            var currentTimestamp = TimeSimulator.GetCurrentTimestamp();
            var targetTimestamp = currentTimestamp - secondsAgo;

            // Handle current time (extrapolate from last observation)
            if (secondsAgo == 0)
            {
                var last = pool.Observations[pool.ObservationIndex];
                var timeDelta1 = currentTimestamp - last.BlockTimestamp;

                var tickCum = last.TickCumulative + (pool.CurrentTick.TickIndex * (long)timeDelta1);
                var secPerLiq = last.SecondsPerLiquidityCumulative +
                    (pool.ActiveLiquidity > 0 ? (decimal)timeDelta1 / pool.ActiveLiquidity : 0m);

                return (tickCum, secPerLiq);
            }

            // Find the observation at or before target time
            var (observationBefore, observationAfter) = GetSurroundingObservations(
                pool, targetTimestamp);

            if (observationBefore.BlockTimestamp == targetTimestamp)
            {
                // Exact match
                return (observationBefore.TickCumulative,
                        observationBefore.SecondsPerLiquidityCumulative);
            }

            // Interpolate between observations
            var timeDelta = targetTimestamp - observationBefore.BlockTimestamp;
            var timeTotal = observationAfter.BlockTimestamp - observationBefore.BlockTimestamp;

            var tickCumulative = observationBefore.TickCumulative +
                (long)((observationAfter.TickCumulative - observationBefore.TickCumulative) *
                timeDelta / timeTotal);

            var secondsPerLiquidityCumulative = observationBefore.SecondsPerLiquidityCumulative +
                (observationAfter.SecondsPerLiquidityCumulative - observationBefore.SecondsPerLiquidityCumulative) *
                timeDelta / timeTotal;

            return (tickCumulative, secondsPerLiquidityCumulative);
        }

        private static (Observation before, Observation after) GetSurroundingObservations(
            PoolV3 pool, uint targetTimestamp)
        {
            // Simplified: linear search through observations
            // Real implementation uses binary search

            Observation before = pool.Observations[0];
            Observation after = pool.Observations[0];

            for (int i = 0; i < pool.ObservationCardinality; i++)
            {
                var obs = pool.Observations[i];
                if (!obs.Initialized)
                    continue;

                if (obs.BlockTimestamp <= targetTimestamp)
                {
                    if (before.BlockTimestamp <= obs.BlockTimestamp)
                        before = obs;
                }

                if (obs.BlockTimestamp >= targetTimestamp)
                {
                    if (after.BlockTimestamp >= obs.BlockTimestamp || after.BlockTimestamp < targetTimestamp)
                        after = obs;
                }
            }

            return (before, after);
        }

        public static decimal CalculateTWAP(this PoolV3 pool, uint secondsAgoStart, uint secondsAgoEnd)
        {
            var (tickCumulativeStart, _) = pool.Observe(secondsAgoStart);
            var (tickCumulativeEnd, _) = pool.Observe(secondsAgoEnd);

            var timeDelta = secondsAgoStart - secondsAgoEnd;
            if (timeDelta == 0)
                throw new ArgumentException("Time delta cannot be zero");

            var averageTick = (tickCumulativeEnd - tickCumulativeStart) / (long)timeDelta;

            return ((int)averageTick).TickToPrice();
        }
    }
}
namespace Uniswap.V3.Lib.Models;

public class TickStates
{
    public TickStates() => (Current) = (null);
    public Tick Current { get; set; }

    public void AddTick(Tick tick)
    {
        if (Current is null)
        {
            Current = tick;
            return; 
        }

        var iterator = Current; 
        
        while (true)
        {
            if (iterator.TickIndex == tick.TickIndex)
            {
                iterator.AddToThis(tick);
                return; 
            }

            if (tick.TickIndex < iterator.TickIndex)
            {
                if (iterator.Previous is null)
                {
                    iterator.SetPrevious(tick);
                    tick.SetNext(iterator);
                    return; 
                }

                if (tick.TickIndex <= iterator.Previous.TickIndex)
                {
                    iterator = iterator.Previous;
                    continue; 
                }

                tick.SetNext(iterator);
                tick.SetPrevious(iterator.Previous); 
                
                iterator.Previous.SetNext(tick);
                iterator.SetPrevious(tick);
                return; 
            }

            if (tick.TickIndex > iterator.TickIndex)
            {
                if (iterator.Next is null)
                {
                    iterator.SetNext(tick);
                    tick.SetPrevious(iterator);
                    return;
                }

                if (tick.TickIndex >= iterator.Next.TickIndex)
                {
                    iterator = iterator.Next;
                    continue;
                }

                tick.SetPrevious(iterator);
                tick.SetNext(iterator.Next);

                iterator.Next.SetPrevious(tick);
                iterator.SetNext(tick);
                return; 
            }
        }
    }

    public bool TryGetTickAtIndex(int tickIndex, out Tick tick)
    {
        Func<Tick, Tick> tickAction = tick => tickIndex >= Current.TickIndex ?  tick.Next : tick.Previous;

        var iterator = Current; 
        while (true)
        {
            if (iterator is null)
            {
                tick = null;
                return false;
            }

            if (iterator.TickIndex == tickIndex)
            {
                tick = iterator;
                return true;
            }

            iterator = tickAction(iterator);
        }
    }
}


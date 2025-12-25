using Uniswap.V3.Lib.Enums;
using Uniswap.V3.Lib.Extensions;

namespace Uniswap.V3.Lib.Models;

public record struct PoolV3
{
    public PoolV3(Token[] tokens, int feeTier, int tickSpacing)
    {
        Tokens = tokens;
        FeeTier = feeTier;
        TickSpacing = tickSpacing;
        SqrtPrice = 0;
        CurrentTick = default(Tick);
        TickStates = [];
        Positions = [];
        FeeGrowthGlobal = [0, 0];
        ProtocolFees = [0, 0]; 
        ActiveLiquidity = 0;
        _initialized = false;
    }

    private bool _initialized; 
    
    //Identity / config
    public Token[] Tokens { get; init; }
    public int FeeTier { get; init; }
    public int TickSpacing { get; init; }

    //Current state
    public decimal SqrtPrice { get; set; }
    public Tick CurrentTick { get; set; }
    public decimal ActiveLiquidity { get; set; }

    //Ownership
    private Dictionary<Tick, TickState> TickStates { get; set; }
    private HashSet<Position> Positions { get; set; } 

    //Global Fees
    public decimal[] FeeGrowthGlobal { get; set; }
    public decimal[] ProtocolFees { get; set; }

    public void Initialize(decimal initialPrice)
    {
        if (_initialized)
            throw new ArgumentException("Already initialized.");
        
        if (initialPrice <= 0)
            throw new ArgumentOutOfRangeException("Price non-positive...");

        CurrentTick = new Tick(initialPrice.PriceToTick());
        SqrtPrice = initialPrice.ToSqrtPrice();
        
        _initialized = true;
    }

    public bool Initialized => _initialized;

    public void AddPosition(Position position)
    {
        if (!Positions.Add(position))
            throw new InvalidOperationException("Position could not be added."); 
    }

    public void AddTick(Tick tick, TickState state = TickState.Initialized)
    {
        if (TickStates.TryGetValue(tick, out var currentTickState))
        {
            if (currentTickState == TickState.DeInitialized)
            {
                TickStates[tick] = state;
            }
            else
            {
                //do nothing??? 
            }
            return; 
        }
        
        TickStates[tick] = state;
    }
}

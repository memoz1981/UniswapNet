using Uniswap.V3.Lib.Enums;

namespace Uniswap.V3.Lib.Models;

public record struct Pool
{
    public Pool(Token[] tokens, int feeTier, int tickSpacing)
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
    }
    
    //Identity / config
    public Token[] Tokens { get; init; }
    public int FeeTier { get; init; }
    public int TickSpacing { get; init; }

    //Current state
    public decimal SqrtPrice { get; set; }
    public Tick CurrentTick { get; set; }
    public decimal ActiveLiquidity { get; set; }

    //Ownership
    public Dictionary<Tick, TickState> TickStates { get; set; }
    public HashSet<Position> Positions { get; set; } 

    //Global Fees
    public decimal[] FeeGrowthGlobal { get; set; }
    public decimal[] ProtocolFees { get; set; }
}

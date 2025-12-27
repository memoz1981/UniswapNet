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
    public decimal SqrtPrice { get; private set; }
    public Tick CurrentTick { get; private set; }
    public decimal ActiveLiquidity { get; private set; }

    //Ownership
    private Dictionary<int, (Tick tick, TickState tickState)> TickStates { get; set; }
    private HashSet<Position> Positions { get; set; }

    //Global Fees
    public decimal[] FeeGrowthGlobal { get; private set; }
    public decimal[] ProtocolFees { get; private set; }

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

    public int Mint(int lpId, int tickMin, int tickMax, decimal liquidity)
    {
        if (CurrentTick.TickIndex >= tickMin && CurrentTick.TickIndex < tickMax)
            ActiveLiquidity += liquidity; 
        
        var tickLower = AddTick(tickMin, liquidity, liquidity);
        var tickUpper = AddTick(tickMax, liquidity, -liquidity);

        var positionId = AddPosition(lpId, liquidity, tickLower, tickUpper);

        return positionId; 
    }

    private int AddPosition(int lpId, decimal liquidity, Tick tickLower, Tick tickUpper)
    {
        var feeGrowthInside = GetFeeGrowthInsideForPosition(tickLower, tickUpper);

        var position = new Position(lpId, tickLower.TickIndex, tickUpper.TickIndex, liquidity, feeGrowthInside);

        if (!Positions.Add(position))
            throw new InvalidOperationException("Position could not be added.");

        return position.Id; 
    }

    private decimal[] GetFeeGrowthInsideForPosition(Tick tickLower, Tick tickUpper)
    {
        decimal feeBelow0 = 0m;
        decimal feeBelow1 = 0m;

        decimal feeAbove0 = 0m;
        decimal feeAbove1 = 0m;

        // assign lower fee
        if (tickLower.TickIndex <= CurrentTick.TickIndex)
        {
            feeBelow0 = tickLower.FeeGrowthOutside[0];
            feeBelow1 = tickLower.FeeGrowthOutside[1];
        }
        else
        {
            feeBelow0 = FeeGrowthGlobal[0] - tickLower.FeeGrowthOutside[0];
            feeBelow1 = FeeGrowthGlobal[1] - tickLower.FeeGrowthOutside[1];
        }

        //assign upper fee
        if (CurrentTick.TickIndex < tickUpper.TickIndex)
        {
            feeAbove0 = tickUpper.FeeGrowthOutside[0];
            feeAbove1 = tickUpper.FeeGrowthOutside[1];
        }
        else
        {
            feeAbove0 = FeeGrowthGlobal[0] - tickUpper.FeeGrowthOutside[0];
            feeAbove1 = FeeGrowthGlobal[1] - tickUpper.FeeGrowthOutside[1];
        }

        var feeGrowthInside0 = FeeGrowthGlobal[0] - feeBelow0 - feeAbove0; 
        var feeGrowthInside1 = FeeGrowthGlobal[1] - feeBelow1 - feeAbove1;

        return [feeGrowthInside0, feeGrowthInside1]; 
    }

    private Tick AddTick(int tickIndex, decimal liquidityGross, decimal liquidityNet)
    {
        var feeGrowth0 = tickIndex <= CurrentTick.TickIndex ? FeeGrowthGlobal[0] : 0; 
        var feeGrowth1 = tickIndex <= CurrentTick.TickIndex ? FeeGrowthGlobal[1] : 0;

        var tickToAdd = new Tick(tickIndex, liquidityGross, liquidityNet, [feeGrowth0, feeGrowth1]);

        if (TickStates.TryGetValue(tickIndex, out var currentTick))
            TickStates[tickIndex] = (currentTick.tick.AddToThis(tickToAdd), TickState.Initialized);
        else
            TickStates[tickIndex] = (tickToAdd, TickState.Initialized);

        return TickStates[tickIndex].tick; 
    }
}

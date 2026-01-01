using Uniswap.V3.Lib.Extensions;

namespace Uniswap.V3.Lib.Models;

public record struct PoolV3
{
    public PoolV3(Token[] tokens, int feeTier, int tickSpacing, int protocolFee)
    {
        Tokens = tokens;
        FeeTier = feeTier;
        TickSpacing = tickSpacing;
        SqrtPrice = 0;
        CurrentTick = default(Tick);
        TickStates = new();
        Positions = [];
        FeeGrowthGlobal = [0, 0];
        ProtocolFees = [0, 0];
        ActiveLiquidity = 0;
        _initialized = false;
        ProtocolFee = protocolFee;
    }

    private bool _initialized;

    //Identity / config
    public Token[] Tokens { get; init; }
    public int FeeTier { get; init; }
    public int TickSpacing { get; init; }
    //actual fee is protocolFee/256... 
    public int ProtocolFee { get; init; }

    //Current state
    public decimal SqrtPrice { get; set; }
    public Tick CurrentTick { get; set; }
    public decimal ActiveLiquidity { get; set; }

    //Ownership
    public TickStates TickStates { get; set; }
    public Dictionary<int, PoolV3Position> Positions { get; set; }

    //Global Fees
    public decimal[] FeeGrowthGlobal { get; set; }
    public decimal[] ProtocolFees { get; set; }

    // TWAP Oracle
    public Observation[] Observations { get; set; }
    public ushort ObservationIndex { get; set; }
    public ushort ObservationCardinality { get; set; }
    public ushort ObservationCardinalityNext { get; set; }

    public void Initialize(decimal initialPrice)
    {
        if (_initialized)
            throw new ArgumentException("Already initialized.");

        if (initialPrice <= 0)
            throw new ArgumentOutOfRangeException("Price non-positive...");

        CurrentTick = new Tick(initialPrice.PriceToTick());
        SqrtPrice = initialPrice.ToSqrtPrice();

        // Initialize observations array
        Observations = new Observation[1];
        Observations[0] = new Observation(
            blockTimestamp: GetCurrentTimestamp(),
            tickCumulative: 0,
            secondsPerLiquidityCumulative: 0m,
            initialized: true
        );
        ObservationIndex = 0;
        ObservationCardinality = 1;
        ObservationCardinalityNext = 1;

        _initialized = true;
    }

    private static uint GetCurrentTimestamp()
    {
        // For testing, you can use a simulated timestamp
        // In production, this would be block.timestamp
        return (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public bool Initialized => _initialized; 
}

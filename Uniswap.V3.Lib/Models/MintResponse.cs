namespace Uniswap.V3.Lib.Models;

public abstract class MintResponse
{
    protected MintResponse(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    public bool IsSuccess { get; }
}

public class RejectedMintResponse : MintResponse
{
    public RejectedMintResponse(string errorMessage) : base(false)
    {
        ErrorMessage = errorMessage;
    }

    public string ErrorMessage { get; }
}

public class AcceptedMintResponse : MintResponse
{
    public AcceptedMintResponse(int positionId, decimal priceMin, decimal priceMax,
        decimal[] tokenAmounts, decimal liquidity) : base(true)
    {
        PositionId = positionId;
        PriceMin = priceMin;
        PriceMax = priceMax;
        TokenAmounts = tokenAmounts;
        Liquidity = liquidity;
    }

    public int PositionId { get; }
    public decimal PriceMin { get; }
    public decimal PriceMax { get; }
    public decimal[] TokenAmounts { get; }
    public decimal Liquidity { get; }
}

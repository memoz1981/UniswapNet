namespace Uniswap.V3.Lib.Models;

public abstract class BurnResponse
{
    protected BurnResponse(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    public bool IsSuccess { get; }
}

public class RejectedBurnResponse : BurnResponse
{
    public RejectedBurnResponse(string errorMessage) : base(false)
    {
        ErrorMessage = errorMessage;
    }

    public string ErrorMessage { get; }
}

public class AcceptedBurnResponse : BurnResponse
{
    public AcceptedBurnResponse(int positionId, decimal[] tokenAmountsBurned, decimal liquidityLeft) : base(true)
    {
        PositionId = positionId;
        TokenAmountsBurned = [tokenAmountsBurned[0], tokenAmountsBurned[1]];
        LiquidityLeft = liquidityLeft;
    }

    public int PositionId { get; }
    public decimal[] TokenAmountsBurned { get; }
    public decimal LiquidityLeft { get; }
}

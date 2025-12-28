namespace Uniswap.V3.Lib.Models;

public abstract class SwapResponse
{
    protected SwapResponse(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    public bool IsSuccess { get; set; }
}

public class RejectedSwapResponse : SwapResponse
{
    public string ErrorMessage { get; set; }

    public RejectedSwapResponse(string errorMessage) : base(false) 
    {
        ErrorMessage = errorMessage;
    }
}

public class AcceptedSwapResponse : SwapResponse
{
    public AcceptedSwapResponse(decimal amountInUsed, decimal amountOutReceived) : base(true)
    {
        AmountInUsed = amountInUsed;
        AmountOutReceived = amountOutReceived;
    }

    public decimal AmountInUsed { get; set; }
    public decimal AmountOutReceived { get; set; }
}

namespace Uniswap.V3.Lib.Models;

public abstract class FlashResponse
{
    protected FlashResponse(bool isSuccess) => IsSuccess = isSuccess;
    public bool IsSuccess { get; }
}

public class RejectedFlashResponse : FlashResponse
{
    public RejectedFlashResponse(string errorMessage) : base(false)
    {
        ErrorMessage = errorMessage;
    }
    public string ErrorMessage { get; }
}

public class AcceptedFlashResponse : FlashResponse
{
    public AcceptedFlashResponse(decimal amount0Flashed, decimal amount1Flashed,
        decimal fee0Paid, decimal fee1Paid) : base(true)
    {
        Amount0Flashed = amount0Flashed;
        Amount1Flashed = amount1Flashed;
        Fee0Paid = fee0Paid;
        Fee1Paid = fee1Paid;
    }

    public decimal Amount0Flashed { get; }
    public decimal Amount1Flashed { get; }
    public decimal Fee0Paid { get; }
    public decimal Fee1Paid { get; }
}

namespace Uniswap.V3.Lib.Models;

public abstract class CollectResponse
{
    protected CollectResponse(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    public bool IsSuccess { get; set; }
}

public class RejectedCollectResponse : CollectResponse
{
    public RejectedCollectResponse(string errorMessage) : base(false)
    {
        ErrorMessage = errorMessage;
    }

    public string ErrorMessage { get; set; }
}

public class AcceptedCollectResponse : CollectResponse
{
    public AcceptedCollectResponse(decimal[] collectedAmounts) : base(true)
    {
        CollectedAmounts = collectedAmounts;
    }

    public decimal[] CollectedAmounts { get; set; }
}

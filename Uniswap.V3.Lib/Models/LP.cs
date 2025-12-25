namespace Uniswap.V3.Lib.Models;

public record struct LP
{
    public static int _count = 0;
    public int Id { get; init; }
    public string Name { get; init; }

    public List<AcceptedMintResponse> Positions { get; private set; }

    public LP(string name)
    {
        Id = _count++;
        Positions = new();
        name = Name;
    }

    public void Mint(PoolV3 pool, MintRequest mintRequest, out bool success, out string errorMessage)
    {
        var response = pool.Mint(mintRequest);
        success = true;
        errorMessage = null;

        if (!response.IsSuccess)
        {
            success = false;

            if(response is not RejectedMintResponse rejectedMintResponse)
                throw new InvalidOperationException("Response mismatch.");

            errorMessage = rejectedMintResponse.ErrorMessage;
        }

        if (response is not AcceptedMintResponse acceptedResponse)
            throw new InvalidOperationException("Response mismatch."); 

        Positions.Add(acceptedResponse);
    }
}

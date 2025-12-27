using Uniswap.V3.Lib.Services;

namespace Uniswap.V3.Lib.Models;

public record struct LP
{
    private readonly PoolMinter _minter;
    public static int _count = 0;
    public int Id { get; init; }
    public string Name { get; init; }

    public List<AcceptedMintResponse> Positions { get; private set; }

    public LP(string name)
    {
        Id = _count++;
        Positions = new();
        name = Name;
        _minter = new();
    }

    public void Mint(PoolV3 pool, MintRequest mintRequest, out bool success, out string errorMessage)
    {
        var response = _minter.Mint(pool, mintRequest); 
        
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

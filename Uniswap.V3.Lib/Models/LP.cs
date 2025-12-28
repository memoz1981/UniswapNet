using Uniswap.V3.Lib.Services;

namespace Uniswap.V3.Lib.Models;

public record struct LP
{
    private readonly PoolMinter _minter;
    private readonly PoolBurner _burner; 
    public static int _count = 0;
    public int Id { get; init; }
    public string Name { get; init; }

    public List<LpPosition> Positions { get; private set; }

    public LP(string name)
    {
        Id = _count++;
        Positions = new();
        name = Name;
        _minter = new();
        _burner = new(); 
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
            return; 
        }

        if (response is not AcceptedMintResponse acceptedResponse)
            throw new InvalidOperationException("Response mismatch."); 

        Positions.Add(new LpPosition(acceptedResponse.PositionId, acceptedResponse.PriceMin, acceptedResponse.PriceMax,
            [0, 0], acceptedResponse.Liquidity));
    }

    public void Burn(PoolV3 pool, int positionId, double percentageToBurn, out bool success, out string errorMessage)
    {
        var positionToBurn = Positions.SingleOrDefault(pos => pos.PositionId == positionId); 

        if (positionToBurn == default)
            throw new ArgumentException($"Position with id {positionId} doesnt exist.");

        if(positionToBurn.Liquidity == 0m)
            throw new ArgumentException($"Position with id {positionId} has no left liquidity to burn.");

        if (percentageToBurn > 100 || percentageToBurn < 0)
            throw new ArgumentException($"Incorrect percentage value {percentageToBurn} passed.");

        var burnRequest = new BurnRequest(Id, positionId, positionToBurn.Liquidity * (decimal)percentageToBurn / 100m); 

        var response = _burner.Burn(pool, burnRequest);

        success = true;
        errorMessage = null;

        if (!response.IsSuccess)
        {
            success = false;

            if (response is not RejectedBurnResponse rejectedMintResponse)
                throw new InvalidOperationException("Response mismatch.");

            errorMessage = rejectedMintResponse.ErrorMessage;
            return; 
        }

        if (response is not AcceptedBurnResponse acceptedResponse)
            throw new InvalidOperationException("Response mismatch.");

        positionToBurn.Liquidity = acceptedResponse.LiquidityLeft;

        positionToBurn.TokensOwed[0] += acceptedResponse.TokenAmountsBurned[0];
        positionToBurn.TokensOwed[1] += acceptedResponse.TokenAmountsBurned[1];
    }
}

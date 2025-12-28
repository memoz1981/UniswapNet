using Uniswap.V3.Lib.Persistence;
using Uniswap.V3.Lib.Services;

namespace Uniswap.V3.Lib.Models;

public record struct LP
{
    private readonly PoolMinter _minter;
    private readonly PoolBurner _burner;
    private readonly PoolCollector _collector; 
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
        _collector = new(); 
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

    public void Collect(PoolV3 pool, decimal[] amounts, int positionId, int recipientId, out bool success, out string errorMessage)
    {
        success = true;
        errorMessage = string.Empty;

        if (!RecipientRepo.RecipientsById.TryGetValue(recipientId, out var recipient))
        {
            success = false;
            errorMessage = $"Recipient with Id of {recipientId} doesn't exist.";

            return; 
        }

        if (amounts is null || amounts.Length != 2 || amounts[0] < 0 || amounts[1] < 0)
        {
            success = false;
            errorMessage = $"Provided amounts not correct.";

            return;
        }

        var positionToCollect = Positions.SingleOrDefault(pos => pos.PositionId == positionId);

        if (positionToCollect == default)
        {
            success = false;
            errorMessage = $"Provided position doesn't exist.";

            return;
        }

        var collectRequest = new CollectRequest(Id, positionId, amounts, recipientId);

        var result = _collector.Collect(pool, collectRequest);

        if (!result.IsSuccess)
        {
            if (result is not RejectedCollectResponse rejectedResponse)
                throw new ArgumentException("Mismatched collect response.");

            errorMessage = rejectedResponse.ErrorMessage;
            success = false;

            return;
        }

        if (result is not AcceptedCollectResponse acceptedResponse)
            throw new ArgumentException("Mismatched collect response.");

        positionToCollect.TokensOwed[0] -= acceptedResponse.CollectedAmounts[0];
        positionToCollect.TokensOwed[1] -= acceptedResponse.CollectedAmounts[1];

        // position no longer needed...
        if (positionToCollect.Liquidity == 0m &&
            positionToCollect.TokensOwed[0] == 0m &&
            positionToCollect.TokensOwed[1] == 0m)
        {
            Positions.Remove(positionToCollect);
        }
    }
}

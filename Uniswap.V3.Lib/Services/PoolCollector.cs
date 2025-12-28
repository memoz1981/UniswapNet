using Uniswap.V3.Lib.Models;
using Uniswap.V3.Lib.Persistence;

namespace Uniswap.V3.Lib.Services;

public class PoolCollector
{
    public CollectResponse Collect(PoolV3 pool, CollectRequest request)
    {
        if (!pool.Initialized)
            return new RejectedCollectResponse("Pool is not initialized.");

        if (request.amounts is null || request.amounts.Length != 2)
            return new RejectedCollectResponse("Amounts to collect not correctly provided.");

        if (!pool.Positions.TryGetValue(request.PositionId, out var position) || position.LpId != request.LpId)
            return new RejectedCollectResponse("Wrong position called for collect.");

        if (request.amounts[0] < 0m || request.amounts[1] < 0m)
            return new RejectedCollectResponse("Both amounts should be non-negative");

        if (!RecipientRepo.RecipientsById.TryGetValue(request.recipientId, out var recipient))
            return new RejectedCollectResponse("Wrong recipient");

        var amountToCollect0 = Math.Min(position.TokensOwed[0], request.amounts[0]);
        var amountToCollect1 = Math.Min(position.TokensOwed[1], request.amounts[1]);

        if (amountToCollect0 != 0)
        {
            if(!recipient.CanSuccessfullyReceive)
                return new RejectedCollectResponse("Couldn't send token 0 to the recipient.");

            recipient.Receive(pool.Tokens[0], amountToCollect0);
        }

        if (amountToCollect1 != 0)
        {
            if (!recipient.CanSuccessfullyReceive)
                return new RejectedCollectResponse("Couldn't send token 1 to the recipient.");

            recipient.Receive(pool.Tokens[1], amountToCollect1);
        }

        position.TokensOwed[0] -= amountToCollect0;
        position.TokensOwed[1] -= amountToCollect1;

        if (position.Liquidity == 0m && position.TokensOwed[0] == 0m && position.TokensOwed[1] == 0m)
        {
            pool.Positions.Remove(position.Id);
        }

        return new AcceptedCollectResponse([amountToCollect0, amountToCollect1]);
    }
}

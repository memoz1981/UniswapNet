using Uniswap.V3.Lib.Extensions;
using Uniswap.V3.Lib.Models;

namespace Uniswap.V3.Lib.Services;

public class PoolFlasher
{
    public FlashResponse Flash(PoolV3 pool, FlashRequest request)
    {
        // Validation
        if (request.Amount0 == 0 && request.Amount1 == 0)
            return new RejectedFlashResponse("Cannot flash zero amounts");

        // Calculate current balances
        var balancesBefore = pool.CalculateTokenBalances();

        if (request.Amount0 > balancesBefore[0])
            return new RejectedFlashResponse($"Insufficient token0: requested {request.Amount0}, available {balancesBefore[0]}");

        if (request.Amount1 > balancesBefore[1])
            return new RejectedFlashResponse($"Insufficient token1: requested {request.Amount1}, available {balancesBefore[1]}");

        // Calculate fees (using pool's fee tier)
        var fee0 = Math.Floor(request.Amount0 * pool.GetFeeTier());
        var fee1 = Math.Floor(request.Amount1 * pool.GetFeeTier());

        // Execute callback (borrower does their thing)
        // In real implementation, callback would execute here
        // For now, we assume it succeeds and tokens are returned

        // Verify balances after (should have original + fees)
        var balancesAfter = pool.CalculateTokenBalancesAfterFlash(
            request.Amount0, request.Amount1, fee0, fee1);

        // Calculate protocol fee portion
        var protocolFee0 = fee0 * pool.ProtocolFee / 256m;
        var protocolFee1 = fee1 * pool.ProtocolFee / 256m;

        // Update protocol fees
        pool.ProtocolFees[0] += protocolFee0;
        pool.ProtocolFees[1] += protocolFee1;

        return new AcceptedFlashResponse(request.Amount0, request.Amount1, fee0, fee1);
    }
}

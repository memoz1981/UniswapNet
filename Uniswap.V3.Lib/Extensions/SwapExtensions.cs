using Uniswap.V3.Lib.Enums;
using Uniswap.V3.Lib.Models;

namespace Uniswap.V3.Lib.Extensions; 

public static class SwapExtensions
{
    public static SwapType? GetSwapTypeOrNull(this SwapRequest request)
    {
        if (request.swapIn.IsEmpty == request.swapOut.IsEmpty)
            return null;

        if (!request.swapIn.IsEmpty)
            return SwapType.ExactIn;

        if (!request.swapOut.IsEmpty)
            return SwapType.ExactOut;

        return null; 
    }

    public static SwapDirection? GetSwapDirectionOrNull(this SwapRequest request, PoolV3 pool)
    {
        if (request.swapIn.TokenIn == pool.Tokens[0] && request.swapOut.TokenOut == pool.Tokens[1])
            return SwapDirection.Token0To1;

        if (request.swapIn.TokenIn == pool.Tokens[1] && request.swapOut.TokenOut == pool.Tokens[0])
            return SwapDirection.Token1To0;

        return null; 
    }
}

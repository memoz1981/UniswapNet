using Uniswap.V3.Lib.Models;

namespace Uniswap.V3.Lib.Extensions;

public static class TokenExtensions
{
    public static bool IsZero(this Token token, decimal value)
        => value * (decimal)Math.Pow(10, token.Decimals) <= 1; 
}

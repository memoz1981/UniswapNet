namespace Uniswap.V3.Lib.Models;

public record struct MintRequest(int LpId, decimal PriceMin, decimal PriceMax, decimal?[] TokenAmounts);

namespace Uniswap.V3.Lib.Models;

public record struct Token(string Name, Guid Address, string Symbol, int Decimals);

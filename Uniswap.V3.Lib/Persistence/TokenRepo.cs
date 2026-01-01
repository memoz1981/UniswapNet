using Uniswap.V3.Lib.Models;

namespace Uniswap.V3.Lib.Persistence;

public class TokenRepo
{
    public static Token[] Tokens =
        [
            new Token("Nethereum 18", Guid.NewGuid(), "NETH18", 18),
            new Token("Nethereum 10", Guid.NewGuid(), "NETH10", 10),
            new Token("Nethereum 6", Guid.NewGuid(), "NETH6", 6),
            new Token("XYZ", Guid.NewGuid(), "XYZ", 18)
        ];
}

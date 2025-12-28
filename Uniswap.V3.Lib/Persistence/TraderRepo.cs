using Uniswap.V3.Lib.Models;

namespace Uniswap.V3.Lib.Persistence;

public class TraderRepo
{
    public static Trader[] Traders
        = [
            new Trader(1, "Trader 1"),
            new Trader(2, "Trader 2"),
            new Trader(3, "Trader 3"),
            new Trader(4, "Trader 4"),
            new Trader(5, "Trader 5"),
          ];
}

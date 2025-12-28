using Uniswap.V3.Lib.Models;

namespace Uniswap.V3.Lib.Persistence;

public class RecipientRepo
{
    public static Recipient[] Recipients = 
        [
            new Recipient(1, "First"),
            new Recipient(2, "Second"),
            new Recipient(3, "Third"),
            new Recipient(4, "Fourth"),
            new Recipient(5, "Fifth"),
        ];

    public static Dictionary<int, Recipient> RecipientsById = Recipients.ToDictionary(r => r.Id); 
}

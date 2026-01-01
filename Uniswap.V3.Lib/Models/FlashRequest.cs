namespace Uniswap.V3.Lib.Models;

public record struct FlashRequest(
    int RecipientId,
    decimal Amount0,
    decimal Amount1,
    object CallbackData
);

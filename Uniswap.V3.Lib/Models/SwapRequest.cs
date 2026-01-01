namespace Uniswap.V3.Lib.Models; 

public record struct SwapRequest(int traderId, SwapIn swapIn, SwapOut swapOut, Recipient recipient = null);
public record struct SwapIn(Token TokenIn, decimal? AmountIn, decimal? AmountOutMinimum, decimal? PriceLimit)
{
    public bool IsValid => (AmountIn == null && AmountOutMinimum == null && PriceLimit == null)
        || (AmountIn != null && AmountOutMinimum != null && PriceLimit != null);

    public bool IsEmpty => AmountIn is null; 
}
public record struct SwapOut(Token TokenOut, decimal? AmountOut, decimal? AmountInMaximum, decimal? PriceLimit)
{
    public bool IsValid => (AmountOut == null && AmountInMaximum == null && PriceLimit == null)
        || (AmountOut != null && AmountInMaximum != null && PriceLimit != null);

    public bool IsEmpty => AmountOut is null; 
}

namespace Uniswap.V3.Lib.Models; 

public record struct SwapRequest(int traderId, SwapIn swapIn, SwapOut swapOut, Recipient recipient = null);
public record struct SwapIn(Token TokenIn, decimal? AmountIn, decimal? AmountOutMinimum)
{
    public bool IsValid => (AmountIn == null && AmountOutMinimum == null)
        || (AmountIn != null && AmountOutMinimum != null);

    public bool IsEmpty => AmountIn is null; 
}
public record struct SwapOut(Token TokenOut, decimal? AmountOut, decimal? AmountInMaximum)
{
    public bool IsValid => (AmountOut == null && AmountInMaximum == null)
        || (AmountOut != null && AmountInMaximum != null);

    public bool IsEmpty => AmountOut is null; 
}

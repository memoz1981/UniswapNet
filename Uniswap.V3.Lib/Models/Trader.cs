using Uniswap.V3.Lib.Persistence;
using Uniswap.V3.Lib.Services;

namespace Uniswap.V3.Lib.Models;

public class Trader
{
    private readonly PoolSwapper _swapper; 
    public Trader(int id, string name)
    {
        Id = id;
        Name = name;

        Holdings = new(); 
        foreach (var token in TokenRepo.Tokens)
            Holdings[token] = 10_000;
        _swapper = new(); 
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public Dictionary<Token, decimal> Holdings { get; set; }

    public void Swap(PoolV3 pool, SwapIn swapIn, SwapOut swapOut, out bool success, out string errorMessage, 
        Recipient recipient = null)
    {
        if (!Holdings.TryGetValue(swapIn.TokenIn, out var tokenInAmount))
        {
            success = false;
            errorMessage = $"Trader doesn't hold token {swapIn.TokenIn.Name}";
            return; 
        }

        if (!swapIn.IsValid || !swapOut.IsValid)
        {
            success = false;
            errorMessage = $"Swap in/out data not valid...";
            return;
        }

        if (swapIn.IsEmpty == swapOut.IsEmpty)
        {
            success = false;
            errorMessage = $"Swap in/out data not valid...";
            return;
        }

        if (!swapIn.IsEmpty && swapIn.AmountIn > tokenInAmount)
        {
            success = false;
            errorMessage = $"Trader doesn't hold token {swapIn.TokenIn.Name}";
            return;
        }

        if (!swapOut.IsEmpty && swapOut.AmountInMaximum > tokenInAmount)
        {
            success = false;
            errorMessage = $"Trader doesn't hold token {swapIn.TokenIn.Name}";
            return;
        }
        
        var request = new SwapRequest(Id, swapIn, swapOut, recipient);

        var response = _swapper.Swap(pool, request);

        if (!response.IsSuccess)
        {
            if (response is not RejectedSwapResponse rejectedResponse)
                throw new InvalidOperationException("Type mismatch...");

            success = false;
            errorMessage = rejectedResponse.ErrorMessage;
            return;
        }

        if(response is not AcceptedSwapResponse acceptedResponse)
            throw new InvalidOperationException("Type mismatch...");

        success = true;
        errorMessage = string.Empty;

        Holdings[swapIn.TokenIn] -= acceptedResponse.AmountInUsed;

        if (recipient is not null)
            return;

        if (!Holdings.ContainsKey(swapOut.TokenOut))
            Holdings[swapOut.TokenOut] = 0m;

        Holdings[swapOut.TokenOut] += acceptedResponse.AmountOutReceived;
    }

    public bool Receive(Token token, decimal amount)
    {
        if (!CanSuccessfullyReceive)
            return false;

        var amountToAdd = amount;

        if (Holdings.TryGetValue(token, out var existingAmount))
        {
            amountToAdd += existingAmount;
        }

        Holdings[token] = amountToAdd;

        return true;
    }

    public bool CanSuccessfullyReceive { get; set; }
}

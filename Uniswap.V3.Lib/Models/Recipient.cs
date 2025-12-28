namespace Uniswap.V3.Lib.Models;

public class Recipient
{
    public Recipient(int id, string name)
    {
        Id = id;
        Name = name;
        Holdings = new(); 
    }

    public int Id { get; set; }
    public string Name { get; set; }

    public Dictionary<Token, decimal> Holdings { get; private set; }

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

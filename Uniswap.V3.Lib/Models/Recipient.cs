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

    public void Receive(Token token, decimal amount)
    {
        if (!Holdings.TryGetValue(token, out var _))
        {
            Holdings[token] = amount;
            return; 
        }
            

        Holdings[token] += amount;
    }
}

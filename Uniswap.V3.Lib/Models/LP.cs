namespace Uniswap.V3.Lib.Models;

public record struct LP
{
    public static int _count = 0;
    public int Id { get; init; }
    public string Name { get; init; }

    public List<Position> Positions { get; private set; }

    public LP(string name)
    {
        Id = _count++;
        Positions = new();
        name = Name;
    }

    public void AddPosition(Position position)
    {
        Positions.Add(position);
    }
}

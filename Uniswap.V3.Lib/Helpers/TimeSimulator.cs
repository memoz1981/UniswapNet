namespace Uniswap.V3.Lib.Helpers;

public static class TimeSimulator
{
    private static uint _currentTimestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static uint GetCurrentTimestamp() => _currentTimestamp;

    public static void AdvanceTime(uint seconds)
    {
        _currentTimestamp += seconds;
    }

    public static void SetTimestamp(uint timestamp)
    {
        _currentTimestamp = timestamp;
    }
}


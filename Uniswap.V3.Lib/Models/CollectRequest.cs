namespace Uniswap.V3.Lib.Models;

public record struct CollectRequest(int LpId, int PositionId, decimal[] amounts, int recipientId); 

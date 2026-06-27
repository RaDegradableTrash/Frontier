public struct FrontlineControlResult
{
    public bool HasController;
    public PlayerSide Controller;
}

public static class FrontlineControlRules
{
    public static FrontlineControlResult Resolve(bool hasPlayerUnit, bool hasEnemyUnit)
    {
        if (hasPlayerUnit == hasEnemyUnit)
        {
            return new FrontlineControlResult();
        }

        return new FrontlineControlResult
        {
            HasController = true,
            Controller = hasPlayerUnit ? PlayerSide.Player : PlayerSide.Enemy
        };
    }
}

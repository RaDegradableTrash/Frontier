public static class KreditRules
{
    public const int MaximumKredits = 12;

    public static int NextMaxKredits(int currentMaxKredits)
    {
        return currentMaxKredits < MaximumKredits ? currentMaxKredits + 1 : MaximumKredits;
    }

    public static int RefilledKredits(int maxKredits)
    {
        return maxKredits < 0 ? 0 : maxKredits;
    }

    public static bool CanSpend(int availableKredits, int cost)
    {
        return cost >= 0 && availableKredits >= cost;
    }

    public static int Spend(int availableKredits, int cost)
    {
        return CanSpend(availableKredits, cost) ? availableKredits - cost : availableKredits;
    }
}

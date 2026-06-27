using System;

public static class CountermeasureRulesTests
{
    public static int Main()
    {
        RuntimeCard attacker = Unit("Attacker", 3, 3);
        RuntimeCard damageTrap = Countermeasure("Damage Trap", CardEffectType.DamageTargetUnit, 2);

        CountermeasureResult damageResult = CountermeasureRules.Resolve(damageTrap, attacker);
        AssertEqual(1, attacker.CurrentDefense, "Damage countermeasure should damage the attacker.");
        AssertTrue(!damageResult.CancelsAttack, "Damage countermeasure should not cancel combat unless it destroys the attacker.");

        RuntimeCard secondAttacker = Unit("Second Attacker", 3, 3);
        RuntimeCard cancelTrap = Countermeasure("Retreat Trap", CardEffectType.CancelAttack, 0);

        CountermeasureResult cancelResult = CountermeasureRules.Resolve(cancelTrap, secondAttacker);
        AssertEqual(3, secondAttacker.CurrentDefense, "Cancel countermeasure should not damage by default.");
        AssertTrue(cancelResult.CancelsAttack, "Cancel countermeasure should stop the attack before combat.");
        return 0;
    }

    private static RuntimeCard Unit(string name, int attack, int defense)
    {
        return new RuntimeCard
        {
            CardName = name,
            Type = CardType.Unit,
            Attack = attack,
            Defense = defense,
            CurrentDefense = defense
        };
    }

    private static RuntimeCard Countermeasure(string name, CardEffectType effectType, int effectAmount)
    {
        return new RuntimeCard
        {
            CardName = name,
            Type = CardType.Countermeasure,
            EffectType = effectType,
            EffectAmount = effectAmount
        };
    }

    private static void AssertEqual(int expected, int actual, string message)
    {
        if (expected != actual)
        {
            throw new Exception($"{message} Expected {expected}, got {actual}.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }
}

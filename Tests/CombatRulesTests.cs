using System;

public static class CombatRulesTests
{
    public static int Main()
    {
        RuntimeCard attacker = Unit("Attacker", 3, 4);
        RuntimeCard defender = Unit("Defender", 2, 5);
        CombatResolution resolution = CombatRules.Plan(attacker, defender);
        AssertEqual(3, resolution.DamageToDefender, "Attacker should deal full damage to defender.");
        AssertEqual(2, resolution.DamageToAttacker, "Defender should deal full counter damage.");
        AssertTrue(!resolution.AmbushFirstStrike, "Normal combat should not trigger ambush.");

        RuntimeCard ambushDefender = Unit("Ambush", 4, 3, CardKeyword.Ambush);
        RuntimeCard weakAttacker = Unit("Weak", 1, 2);
        CombatResolution ambushResolution = CombatRules.Plan(weakAttacker, ambushDefender);
        AssertTrue(ambushResolution.AmbushFirstStrike, "Ambush defender should trigger first strike.");
        AssertEqual(4, ambushResolution.DamageToAttacker, "Ambush counter damage should equal defender attack.");
        AssertTrue(CombatRules.ShouldSkipDuplicateCounterDamage(true), "Ambush should not apply duplicate counter damage.");
        AssertTrue(!CombatRules.AttackerSurvivesAmbush(weakAttacker, ambushResolution.DamageToAttacker), "Weak attacker should die to ambush.");

        RuntimeCard heavyDefender = Unit("Heavy", 1, 6, CardKeyword.HeavyArmor);
        AssertEqual(2, CombatRules.ModifiedDamage(3, heavyDefender), "Heavy armor should reduce incoming damage by 1.");
        return 0;
    }

    private static RuntimeCard Unit(string name, int attack, int defense, CardKeyword keywords = CardKeyword.None)
    {
        return new RuntimeCard
        {
            CardName = name,
            Type = CardType.Unit,
            Attack = attack,
            Defense = defense,
            CurrentDefense = defense,
            Keywords = keywords
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

using System;

public static class DamagePreviewRulesTests
{
    public static int Main()
    {
        RuntimeCard attacker = new RuntimeCard { Type = CardType.Unit, Attack = 4, CurrentDefense = 3, Defense = 3 };
        RuntimeCard defender = new RuntimeCard { Type = CardType.Unit, Attack = 2, CurrentDefense = 5, Defense = 5 };
        DamagePreview preview = DamagePreviewRules.ForUnitAttack(attacker, defender);
        AssertEqual(4, preview.DamageToTarget, "Preview should show attacker damage on defender.");
        AssertTrue(!preview.TargetLethal, "Defender should survive the previewed hit.");
        AssertEqual(2, preview.CounterDamage, "Preview should show counter damage on attacker.");
        AssertTrue(preview.ShowCounter, "Unit combat preview should show counter damage.");

        RuntimeCard order = new RuntimeCard { Type = CardType.Order, EffectType = CardEffectType.DamageTargetUnit, EffectAmount = 3 };
        RuntimeCard target = new RuntimeCard { Type = CardType.Unit, CurrentDefense = 2, Defense = 2 };
        DamagePreview orderPreview = DamagePreviewRules.ForOrder(order, target);
        AssertEqual(3, orderPreview.DamageToTarget, "Order preview should show effect amount.");
        AssertTrue(orderPreview.TargetLethal, "Lethal order preview should mark target death.");
        return 0;
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

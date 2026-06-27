using System;

public static class SlotHighlightLabelRulesTests
{
    public static int Main()
    {
        RuntimeCard handUnit = new RuntimeCard { Type = CardType.Unit, Zone = CardZone.Hand };
        RuntimeCard supportUnit = new RuntimeCard { Type = CardType.Unit, Zone = CardZone.PlayerSupport };
        RuntimeCard frontlineUnit = new RuntimeCard { Type = CardType.Unit, Zone = CardZone.Frontline };
        RuntimeCard guardTarget = new RuntimeCard { Type = CardType.Unit, Zone = CardZone.EnemySupport, Keywords = CardKeyword.Guard };
        RuntimeCard ordinaryTarget = new RuntimeCard { Type = CardType.Unit, Zone = CardZone.EnemySupport };
        RuntimeCard countermeasure = new RuntimeCard { Type = CardType.Countermeasure, Zone = CardZone.Hand };
        RuntimeCard order = new RuntimeCard { Type = CardType.Order, Zone = CardZone.Hand };
        RuntimeCard damageOrder = new RuntimeCard { Type = CardType.Order, Zone = CardZone.Hand, EffectType = CardEffectType.DamageTargetUnit };
        RuntimeCard pinOrder = new RuntimeCard { Type = CardType.Order, Zone = CardZone.Hand, EffectType = CardEffectType.PinTargetUnit };
        RuntimeCard buffOrder = new RuntimeCard { Type = CardType.Order, Zone = CardZone.Hand, EffectType = CardEffectType.BuffFriendlyUnit };
        RuntimeCard hqDamageOrder = new RuntimeCard { Type = CardType.Order, Zone = CardZone.Hand, EffectType = CardEffectType.DamageEnemyHeadquarters };
        RuntimeCard repairOrder = new RuntimeCard { Type = CardType.Order, Zone = CardZone.Hand, EffectType = CardEffectType.RepairHeadquarters };
        RuntimeCard drawOrder = new RuntimeCard { Type = CardType.Order, Zone = CardZone.Hand, EffectType = CardEffectType.DrawCards };

        AssertTrue(SlotHighlightLabelRules.LabelFor(handUnit, SlotZone.PlayerSupport) == "DEPLOY HERE", "Hand units should tell new players where to deploy.");
        AssertTrue(SlotHighlightLabelRules.LabelFor(handUnit, SlotZone.Frontline) == "MOBILIZE", "Mobilize units should label direct frontline deployment distinctly.");
        AssertTrue(SlotHighlightLabelRules.LabelFor(supportUnit, SlotZone.Frontline) == "ADVANCE HERE", "Support units should tell players where to advance.");
        AssertTrue(SlotHighlightLabelRules.LabelFor(frontlineUnit, SlotZone.EnemySupport) == "ATTACK HERE", "Frontline units should label attack targets.");
        AssertTrue(SlotHighlightLabelRules.AttackLabelFor(guardTarget, true) == "ATTACK GUARD", "Guard targets should be labelled as mandatory guard attacks.");
        AssertTrue(SlotHighlightLabelRules.AttackLabelFor(ordinaryTarget, false) == "ATTACK HERE", "Ordinary legal attack targets should keep the normal attack label.");
        AssertTrue(SlotHighlightLabelRules.AttackLabelFor(null, false) == "ATTACK HQ", "Empty legal enemy support targets should clearly label headquarters attacks.");
        AssertTrue(SlotHighlightLabelRules.LabelFor(countermeasure, SlotZone.PlayerSupport) == "SET COUNTER", "Countermeasures should label set targets.");
        AssertTrue(SlotHighlightLabelRules.LabelFor(order, SlotZone.EnemySupport) == "PLAY ORDER", "Orders should label playable targets.");
        AssertTrue(SlotHighlightLabelRules.LabelFor(damageOrder, SlotZone.EnemySupport) == "DAMAGE UNIT", "Damage orders should identify enemy unit targets.");
        AssertTrue(SlotHighlightLabelRules.LabelFor(pinOrder, SlotZone.EnemySupport) == "PIN UNIT", "Pin orders should identify enemy unit targets.");
        AssertTrue(SlotHighlightLabelRules.LabelFor(buffOrder, SlotZone.PlayerSupport) == "BUFF ALLY", "Buff orders should identify friendly unit targets.");
        AssertTrue(SlotHighlightLabelRules.LabelFor(hqDamageOrder, SlotZone.PlayerSupport) == "DAMAGE HQ", "Headquarters damage orders should identify their effect.");
        AssertTrue(SlotHighlightLabelRules.LabelFor(repairOrder, SlotZone.PlayerSupport) == "REPAIR HQ", "Repair orders should identify their effect.");
        AssertTrue(SlotHighlightLabelRules.LabelFor(drawOrder, SlotZone.PlayerSupport) == "DRAW CARDS", "Draw orders should identify their effect.");
        return 0;
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }
}

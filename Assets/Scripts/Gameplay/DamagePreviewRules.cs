public struct DamagePreview
{
    public int DamageToTarget;
    public bool TargetLethal;
    public int CounterDamage;
    public bool AttackerLethal;
    public bool IsCanceled;
    public bool ShowCounter;
    public int AdjacentTargets;
    public int AdjacentDamage;
}

public static class DamagePreviewRules
{
    public static DamagePreview ForUnitAttack(RuntimeCard attacker, RuntimeCard defender)
    {
        return ForUnitAttack(attacker, defender, new CountermeasureResult());
    }

    public static DamagePreview ForUnitAttack(RuntimeCard attacker, RuntimeCard defender, CountermeasureResult countermeasureResult)
    {
        if (attacker == null || defender == null)
        {
            return default;
        }

        if (countermeasureResult.CancelsAttack)
        {
            return new DamagePreview
            {
                IsCanceled = true,
                ShowCounter = countermeasureResult.DamageToAttacker > 0,
                CounterDamage = countermeasureResult.DamageToAttacker
            };
        }

        CombatResolution resolution = CombatRules.Plan(
            attacker,
            defender,
            countermeasureResult.BonusDefenseToTarget,
            countermeasureResult.BonusAttackToTarget,
            countermeasureResult.TargetGainsAmbush);
        bool attackerSurvivesAmbush = !resolution.AmbushFirstStrike
            || CombatRules.AttackerSurvivesAmbush(attacker, resolution.DamageToAttacker);
        int defenderDamage = attackerSurvivesAmbush ? resolution.DamageToDefender : 0;
        int adjustedDefenderDefense = defender.CurrentDefense + countermeasureResult.BonusDefenseToTarget;

        return new DamagePreview
        {
            DamageToTarget = defenderDamage,
            TargetLethal = defenderDamage >= adjustedDefenderDefense,
            CounterDamage = resolution.DamageToAttacker,
            AttackerLethal = resolution.DamageToAttacker >= attacker.CurrentDefense,
            ShowCounter = countermeasureResult.DamageToAttacker > 0,
            IsCanceled = false
        };
    }

    public static DamagePreview ForHeadquartersAttack(RuntimeCard attacker, int headquartersHealth)
    {
        if (attacker == null)
        {
            return default;
        }

        return new DamagePreview
        {
            DamageToTarget = attacker.Attack,
            TargetLethal = attacker.Attack >= headquartersHealth,
            CounterDamage = 0,
            AttackerLethal = false,
            ShowCounter = false
        };
    }

    public static DamagePreview ForOrder(RuntimeCard order, RuntimeCard targetUnit)
    {
        if (order == null || order.Type != CardType.Order)
        {
            return default;
        }

        if ((order.EffectType == CardEffectType.DamageTargetUnit
            || order.EffectType == CardEffectType.DamageTargetUnitAndAdjacent)
            && targetUnit != null)
        {
            int damage = CombatRules.ModifiedDamage(order.EffectAmount, targetUnit);
            return new DamagePreview
            {
                DamageToTarget = damage,
                TargetLethal = damage >= targetUnit.CurrentDefense,
                CounterDamage = 0,
                AttackerLethal = false,
                ShowCounter = false
            };
        }

        if (order.EffectType == CardEffectType.DamageEnemyHeadquarters)
        {
            return new DamagePreview
            {
                DamageToTarget = order.EffectAmount,
                TargetLethal = false,
                CounterDamage = 0,
                AttackerLethal = false,
                ShowCounter = false
            };
        }

        return default;
    }
}

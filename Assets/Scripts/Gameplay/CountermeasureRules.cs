using UnityEngine;

public static class CountermeasureRules
{
    public static CountermeasureResult Resolve(RuntimeCard countermeasure, RuntimeCard attacker, RuntimeCard attackedUnit)
    {
        CountermeasureResult result = Predict(countermeasure, attacker, attackedUnit);
        if (!result.Triggered || attacker == null)
        {
            return result;
        }

        if (result.DamageToAttacker > 0)
        {
            attacker.CurrentDefense -= result.DamageToAttacker;
            result.CancelsAttack = !attacker.IsAlive;
        }

        if (attackedUnit != null)
        {
            if (result.BonusAttackToTarget > 0)
            {
                attackedUnit.Attack += result.BonusAttackToTarget;
            }

            if (result.BonusDefenseToTarget > 0)
            {
                attackedUnit.CurrentDefense += result.BonusDefenseToTarget;
            }

            if (result.TargetGainsAmbush)
            {
                attackedUnit.AddKeyword(CardKeyword.Ambush);
            }
        }

        return result;
    }

    public static CountermeasureResult Predict(RuntimeCard countermeasure, RuntimeCard attacker, RuntimeCard attackedUnit)
    {
        CountermeasureResult result = new CountermeasureResult();
        if (countermeasure == null || attacker == null)
        {
            return result;
        }

        result.Triggered = true;
        switch (countermeasure.EffectType)
        {
            case CardEffectType.CancelAttack:
                result.CancelsAttack = true;
                break;
            case CardEffectType.DamageTargetUnit:
                result.DamageToAttacker = countermeasure.EffectAmount;
                break;
            case CardEffectType.Trap:
                if (attackedUnit == null)
                {
                    result.Triggered = false;
                    break;
                }

                result.BonusDefenseToTarget = Mathf.Max(0, countermeasure.EffectAmount);
                result.BonusAttackToTarget = 1;
                result.TargetGainsAmbush = true;
                break;
            case CardEffectType.GrantFriendlyDefense:
                result.BonusDefenseToTarget = Mathf.Max(0, countermeasure.EffectAmount);
                break;
        }

        return result;
    }
}

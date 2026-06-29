using UnityEngine;

public static class CountermeasureRules
{
    public static CountermeasureResult Resolve(RuntimeCard countermeasure, RuntimeCard attacker)
    {
        CountermeasureResult result = Predict(countermeasure, attacker);
        if (!result.Triggered || attacker == null)
        {
            return result;
        }

        if (result.DamageToAttacker > 0)
        {
            attacker.CurrentDefense -= result.DamageToAttacker;
            result.CancelsAttack = !attacker.IsAlive;
        }

        return result;
    }

    public static CountermeasureResult Predict(RuntimeCard countermeasure, RuntimeCard attacker)
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

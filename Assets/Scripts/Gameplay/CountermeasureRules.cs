public static class CountermeasureRules
{
    public static CountermeasureResult Resolve(RuntimeCard countermeasure, RuntimeCard attacker)
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
                attacker.CurrentDefense -= countermeasure.EffectAmount;
                result.CancelsAttack = !attacker.IsAlive;
                break;
        }

        return result;
    }
}

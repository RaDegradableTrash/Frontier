using UnityEngine;

public struct CombatResolution
{
    public int DamageToDefender;
    public int DamageToAttacker;
    public bool AmbushFirstStrike;
}

public static class CombatRules
{
    public static int ModifiedDamage(int amount, RuntimeCard target)
    {
        if (target != null && target.HasKeyword(CardKeyword.HeavyArmor))
        {
            return System.Math.Max(0, amount - 1);
        }

        return amount;
    }

    public static CombatResolution Plan(RuntimeCard attacker, RuntimeCard defender)
    {
        return Plan(attacker, defender, 0, 0, false);
    }

    public static CombatResolution Plan(RuntimeCard attacker, RuntimeCard defender, int defenderBonusDefense, int defenderBonusAttack, bool forceDefenderAmbush)
    {
        int adjustedDefenderDefense = defender != null ? defender.CurrentDefense + Mathf.Max(0, defenderBonusDefense) : 0;
        int adjustedDefenderAttack = defender != null ? defender.Attack + Mathf.Max(0, defenderBonusAttack) : 0;

        int damageToDefender = ModifiedDamage(attacker.Attack, defender);
        int damageToAttacker = ModifiedDamage(adjustedDefenderAttack, attacker);
        bool ambushFirstStrike = defender != null
            && (defender.HasKeyword(CardKeyword.Ambush) || forceDefenderAmbush)
            && defender.AttacksThisTurn == 0
            && adjustedDefenderAttack > 0;

        if (ambushFirstStrike && !attacker.IsAlive)
        {
            return new CombatResolution
            {
                DamageToDefender = 0,
                DamageToAttacker = damageToAttacker,
                AmbushFirstStrike = true
            };
        }

        return new CombatResolution
        {
            DamageToDefender = damageToDefender,
            DamageToAttacker = damageToAttacker,
            AmbushFirstStrike = ambushFirstStrike
        };
    }

    public static bool AttackerSurvivesAmbush(RuntimeCard attacker, int ambushDamage)
    {
        return attacker != null && attacker.CurrentDefense > ambushDamage;
    }

    public static bool ShouldApplyCounterDamageAfterAmbush(bool ambushFirstStrike, bool attackerAliveAfterAmbush)
    {
        return !ambushFirstStrike || attackerAliveAfterAmbush;
    }

    public static bool ShouldSkipDuplicateCounterDamage(bool ambushFirstStrike)
    {
        return ambushFirstStrike;
    }
}

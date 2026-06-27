public static class UnitAttackRules
{
    public static void MarkAttackResolved(RuntimeCard attacker)
    {
        if (attacker == null)
        {
            return;
        }

        attacker.AttacksThisTurn++;
        int maxAttacks = attacker.HasKeyword(CardKeyword.Fury) ? 2 : 1;
        attacker.HasActed = attacker.AttacksThisTurn >= maxAttacks;
    }
}

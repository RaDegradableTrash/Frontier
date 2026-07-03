using System.Text;

public static class CardInspectorTextRules
{
    public static string EmptyHint()
    {
        return
            "CARD INTEL\n" +
            "1. HOVER BOTTOM EDGE to raise your hand.\n" +
            "2. CLICK any visible card to inspect it.\n" +
            "3. DRAG UNITS to ANY EMPTY GRID SLOT.\n" +
            "4. CLICK SET COUNTERS to check them.";
    }

    public static string ForCard(RuntimeCard card)
    {
        if (card == null)
        {
            return EmptyHint();
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(card.CardName);
        builder.AppendLine($"COST: {card.KreditCost}   TYPE: {ReadableType(card.Type)}");
        builder.AppendLine($"ZONE: {ReadableZone(card.Zone)}");

        if (card.Type == CardType.Unit)
        {
            builder.AppendLine($"ATTACK: {card.Attack}   DEFENSE: {card.CurrentDefense}   OPERATE: {card.OperationCost}");
        }
        else
        {
            builder.AppendLine($"EFFECT: {ReadableEffect(card.EffectType, card.EffectAmount)}");
        }

        builder.AppendLine($"STATUS: {ReadableStatus(card)}");
        if (!string.IsNullOrEmpty(card.RulesText))
        {
            builder.AppendLine(card.RulesText);
        }

        return builder.ToString();
    }

    private static string ReadableType(CardType type)
    {
        return type == CardType.Countermeasure ? "COUNTERMEASURE" : type.ToString().ToUpperInvariant();
    }

    private static string ReadableZone(CardZone zone)
    {
        switch (zone)
        {
            case CardZone.Hand:
                return "HAND";
            case CardZone.Deck:
                return "DECK";
            case CardZone.Discard:
                return "DISCARD";
            case CardZone.PlayerSupport:
                return "BOARD";
            case CardZone.EnemySupport:
                return "BOARD";
            case CardZone.Frontline:
                return "BOARD";
            case CardZone.Countermeasure:
                return "SET COUNTER";
            default:
                return zone.ToString().ToUpperInvariant();
        }
    }

    private static string ReadableEffect(CardEffectType effectType, int effectAmount)
    {
        switch (effectType)
        {
            case CardEffectType.DamageTargetUnit:
            case CardEffectType.DamageTargetUnitAndAdjacent:
                return $"DAMAGE UNIT {effectAmount}";
            case CardEffectType.DamageEnemyHeadquarters:
                return $"DAMAGE HQ {effectAmount}";
            case CardEffectType.RepairHeadquarters:
                return $"REPAIR HQ {effectAmount}";
            case CardEffectType.DrawCards:
                return $"DRAW {effectAmount}";
            case CardEffectType.BuffFriendlyUnit:
            case CardEffectType.Trap:
                return $"BUFF ALLY {effectAmount}";
            case CardEffectType.PinTargetUnit:
                return "PIN ENEMY UNIT";
            case CardEffectType.CancelAttack:
                return "CANCEL ATTACK";
            default:
                return "NONE";
        }
    }

    private static string ReadableStatus(RuntimeCard card)
    {
        if (card.Type == CardType.Countermeasure && card.Zone == CardZone.Countermeasure)
        {
            return "SET COUNTER";
        }

        if (card.Keywords == CardKeyword.None)
        {
            return "READY";
        }

        StringBuilder builder = new StringBuilder();
        AppendKeyword(builder, card, CardKeyword.Pinned, "PINNED");
        AppendKeyword(builder, card, CardKeyword.Guard, "GUARD");
        AppendKeyword(builder, card, CardKeyword.Blitz, "BLITZ");
        AppendKeyword(builder, card, CardKeyword.Ambush, "AMBUSH");
        AppendKeyword(builder, card, CardKeyword.Fury, "FURY");
        AppendKeyword(builder, card, CardKeyword.Smokescreen, "SMOKESCREEN");
        AppendKeyword(builder, card, CardKeyword.Mobilize, "MOBILIZE");
        AppendKeyword(builder, card, CardKeyword.HeavyArmor, "HEAVY ARMOR");
        return builder.Length == 0 ? "READY" : builder.ToString();
    }

    private static void AppendKeyword(StringBuilder builder, RuntimeCard card, CardKeyword keyword, string label)
    {
        if (!card.HasKeyword(keyword))
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append(", ");
        }

        builder.Append(label);
    }
}

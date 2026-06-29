public static class OrderDragRules
{
    public static bool IsTargetedOrder(RuntimeCard order)
    {
        if (order == null || order.Type != CardType.Order)
        {
            return false;
        }

        return order.EffectType == CardEffectType.DamageTargetUnit
            || order.EffectType == CardEffectType.PinTargetUnit
            || order.EffectType == CardEffectType.BuffFriendlyUnit
            || order.EffectType == CardEffectType.DamageTargetUnitAndAdjacent;
    }

    public static bool ShouldFollowPointer(RuntimeCard order)
    {
        return order != null && order.Type == CardType.Order && !IsTargetedOrder(order);
    }

    public static bool ShouldHoverAboveHand(RuntimeCard order)
    {
        return IsTargetedOrder(order);
    }
}

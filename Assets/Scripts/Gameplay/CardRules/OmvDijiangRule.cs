using UnityEngine;

[CreateAssetMenu(fileName = "OmvDijiangRule", menuName = "Frontier/Card Rules/O.M.V. Dijiang")]
public sealed class OmvDijiangRule : CardRule
{
    public override bool TryResolveOrder(CardRuleExecutionContext context)
    {
        if (context == null || context.Card == null || context.TargetSlot == null || context.Card.EffectType != CardEffectType.DamageTargetUnitAndAdjacent)
        {
            return false;
        }

        RuntimeCard order = context.Card;
        SlotInteract target = context.TargetSlot;
        if (BoardTargetRules.IsHeadquartersSlot(target) && !target.IsOccupied)
        {
            PlayerSide targetSide = target.Zone == SlotZone.EnemySupport ? PlayerSide.Enemy : PlayerSide.Player;
            PlayerState targetState = context.GetState?.Invoke(targetSide);
            if (targetState != null)
            {
                targetState.HeadquartersHealth -= order.EffectAmount;
                context.SpawnFloatingText?.Invoke($"-{order.EffectAmount} HQ", context.HeadquartersMarker.Invoke(targetSide), Color.red, FeedbackCueType.Attack);
            }
        }
        else if (target.IsOccupied)
        {
            context.DamageUnit?.Invoke(target.Occupant, order.EffectAmount, order.CardName);
        }

        if (context.Board != null)
        {
            ResolveAdjacent(context, context.Board.GetSlot(target.X - 1, target.Zone));
            ResolveAdjacent(context, context.Board.GetSlot(target.X + 1, target.Zone));
        }

        context.SetStatus?.Invoke(SceneGuidanceRules.AfterOrderPrompt(order));
        return true;
    }

    private static void ResolveAdjacent(CardRuleExecutionContext context, SlotInteract slot)
    {
        if (slot == null || !slot.IsOccupied || slot.Occupant == null)
        {
            return;
        }

        int damage = context.AdjacentAreaDamage != null ? context.AdjacentAreaDamage(context.Card, slot) : Mathf.Max(0, context.Card.EffectAmount - 2);
        context.DamageUnit?.Invoke(slot.Occupant, damage, context.Card.CardName);
    }
}

using UnityEngine;

[CreateAssetMenu(fileName = "AirborneRule", menuName = "Frontier/Card Rules/Airborne")]
public sealed class AirborneRule : CardRule
{
    public override bool TryResolveOrder(CardRuleExecutionContext context)
    {
        if (context == null || context.Card == null || context.Caster == null || context.Card.EffectType != CardEffectType.DeployWithBlitz)
        {
            return false;
        }

        RuntimeCard unit = context.ChooseAirborneUnit?.Invoke(context.Caster);
        if (unit == null || context.TargetSlot == null || context.IsEmptyAirborneSlot == null || !context.IsEmptyAirborneSlot(context.TargetSlot, context.Caster.Side))
        {
            return true;
        }

        context.DeployAirborneUnit?.Invoke(context.Caster, unit, context.TargetSlot, context.Caster.Side == PlayerSide.Player ? Color.cyan : Color.red);
        context.SetStatus?.Invoke(SceneGuidanceRules.AfterOrderPrompt(context.Card));
        return true;
    }
}

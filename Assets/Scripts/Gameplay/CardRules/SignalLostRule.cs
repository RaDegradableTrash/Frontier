using UnityEngine;

[CreateAssetMenu(fileName = "SignalLostRule", menuName = "Frontier/Card Rules/Signal Lost")]
public sealed class SignalLostRule : CardRule
{
    public override bool TryResolveOrder(CardRuleExecutionContext context)
    {
        if (context == null || context.Card == null || context.Caster == null || context.Card.EffectType != CardEffectType.IncreaseEnemyCosts)
        {
            return false;
        }

        PlayerState affected = context.GetOpponentState?.Invoke(context.Caster.Side);
        int amount = Mathf.Max(1, context.Card.EffectAmount);
        context.ApplyCostIncrease?.Invoke(affected, amount);
        context.FlashCostChange?.Invoke(affected);
        if (affected != null)
        {
            context.SpawnFloatingText?.Invoke("COST +" + amount, context.HeadquartersMarker.Invoke(affected.Side), Color.red, FeedbackCueType.Countermeasure);
        }

        context.SetStatus?.Invoke(SceneGuidanceRules.AfterOrderPrompt(context.Card));
        return true;
    }
}

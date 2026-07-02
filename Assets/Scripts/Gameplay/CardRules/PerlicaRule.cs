using UnityEngine;

[CreateAssetMenu(fileName = "PerlicaRule", menuName = "Frontier/Card Rules/Perlica")]
public sealed class PerlicaRule : CardRule
{
    public override bool TryResolveDeployment(CardRuleExecutionContext context)
    {
        if (context == null || context.Card == null || context.Caster == null || context.Card.EffectType != CardEffectType.AddUnitToHand)
        {
            return false;
        }

        RuntimeCard template = context.FindCardTemplate?.Invoke(context.Card.AddedCardName);
        if (template == null)
        {
            return true;
        }

        RuntimeCard reward = template.CloneFor(context.Caster.Side);
        if (context.AddCardToHand != null)
        {
            context.AddCardToHand(context.Caster, reward);
        }
        else
        {
            reward.Zone = CardZone.Hand;
            context.Caster.Hand.Add(reward);
        }
        Vector3 position = context.TargetSlot != null ? context.TargetSlot.transform.position : Vector3.zero;
        context.SpawnFloatingText?.Invoke("+" + context.Card.AddedCardName, position, Color.yellow, FeedbackCueType.Draw);
        context.SetStatus?.Invoke(context.Card.CardName + " added " + context.Card.AddedCardName + " to hand.");
        return true;
    }
}

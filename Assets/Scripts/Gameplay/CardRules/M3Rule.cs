using UnityEngine;

[CreateAssetMenu(fileName = "M3Rule", menuName = "Frontier/Card Rules/M3")]
public sealed class M3Rule : CardRule
{
    public override bool TryResolveAfterAttack(CardRuleExecutionContext context)
    {
        if (context == null || context.Card == null || !context.Card.IsAlive)
        {
            return false;
        }

        context.GainFriendlyDefense?.Invoke(context.Caster, Mathf.Max(1, context.Card.EffectAmount));
        return true;
    }
}

using UnityEngine;

[CreateAssetMenu(fileName = "GilbertaRule", menuName = "Frontier/Card Rules/Gilberta")]
public sealed class GilbertaRule : CardRule
{
    public override bool TryResolveDeployment(CardRuleExecutionContext context)
    {
        if (context == null || context.Caster == null)
        {
            return false;
        }

        context.Caster.RegisterGilbertaAura();
        return true;
    }
}

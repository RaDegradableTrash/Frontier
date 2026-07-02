using UnityEngine;

public abstract class CardRule : ScriptableObject
{
    [SerializeField] private string ruleId;
    public string RuleId => string.IsNullOrWhiteSpace(ruleId) ? name : ruleId;

    public virtual bool TryResolveOrder(CardRuleExecutionContext context)
    {
        return false;
    }

    public virtual bool TryResolveDeployment(CardRuleExecutionContext context)
    {
        return false;
    }

    public virtual bool TryResolveAfterAttack(CardRuleExecutionContext context)
    {
        return false;
    }
}

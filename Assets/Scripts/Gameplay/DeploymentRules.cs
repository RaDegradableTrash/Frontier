using UnityEngine;

public static class DeploymentRules
{
    public static DeploymentResult Resolve(RuntimeCard card)
    {
        DeploymentResult result = new DeploymentResult();
        if (card == null || card.Type != CardType.Unit || card.Trigger != CardTrigger.Deployment)
        {
            return result;
        }

        result.Triggered = true;
        switch (card.EffectType)
        {
            case CardEffectType.AddUnitToHand:
                result.GiveCardToHand = true;
                result.CardNameToHand = card.AddedCardName;
                break;
            case CardEffectType.DrawCards:
                result.CardsToDraw += card.EffectAmount;
                break;
            case CardEffectType.DeployWithBlitz:
                break;
            case CardEffectType.GrantFriendlyDefense:
                result.FriendlyDefenseGain = card.EffectAmount;
                break;
            case CardEffectType.IncreaseEnemyCosts:
                result.EnemyDeploymentCostIncrease = card.EffectAmount;
                result.EnemyOperationCostIncrease = card.EffectAmount;
                break;
            case CardEffectType.DrawForCardsPlayed:
                result.DrawForCardsPlayed = true;
                result.DrawForCardsPlayedAmount = Mathf.Max(1, card.EffectAmount);
                break;
        }

        return result;
    }
}

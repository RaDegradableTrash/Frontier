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
            case CardEffectType.DrawCards:
                result.CardsToDraw = card.EffectAmount;
                break;
        }

        return result;
    }
}

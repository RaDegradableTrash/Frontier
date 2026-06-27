using System;

public static class DeploymentRulesTests
{
    public static int Main()
    {
        RuntimeCard supplyUnit = Unit("Supply Unit");
        supplyUnit.Trigger = CardTrigger.Deployment;
        supplyUnit.EffectType = CardEffectType.DrawCards;
        supplyUnit.EffectAmount = 1;

        DeploymentResult result = DeploymentRules.Resolve(supplyUnit);
        AssertTrue(result.Triggered, "Deployment unit should trigger when deployed.");
        AssertEqual(1, result.CardsToDraw, "Deployment draw should request one card.");

        RuntimeCard plainUnit = Unit("Plain Unit");
        DeploymentResult plainResult = DeploymentRules.Resolve(plainUnit);
        AssertTrue(!plainResult.Triggered, "Units without deployment text should not trigger.");
        AssertEqual(0, plainResult.CardsToDraw, "Plain units should not draw cards.");

        RuntimeCard order = Unit("Order Template");
        order.Type = CardType.Order;
        order.Trigger = CardTrigger.Deployment;
        order.EffectType = CardEffectType.DrawCards;
        order.EffectAmount = 1;

        DeploymentResult orderResult = DeploymentRules.Resolve(order);
        AssertTrue(!orderResult.Triggered, "Only units should resolve deployment effects.");
        return 0;
    }

    private static RuntimeCard Unit(string name)
    {
        return new RuntimeCard
        {
            CardName = name,
            Type = CardType.Unit,
            CurrentDefense = 2
        };
    }

    private static void AssertEqual(int expected, int actual, string message)
    {
        if (expected != actual)
        {
            throw new Exception($"{message} Expected {expected}, got {actual}.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }
}

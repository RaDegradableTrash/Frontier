using System;

public static class PlayableTurnLoopTests
{
    public static int Main()
    {
        PlayerState player = new PlayerState(PlayerSide.Player);
        player.StartTurn();
        AssertEqual(1, player.Kredits, "Opening player turn should start with one Kredit.");

        RuntimeCard scout = Unit(CardZone.Hand, 1, 1);
        AssertTrue(player.TrySpendKredits(scout.KreditCost), "A one-cost unit should be playable on the opening turn.");
        UnitDeploymentRules.MarkDeployed(scout);
        scout.Zone = CardZone.PlayerSupport;
        AssertTrue(scout.HasActed, "New non-Blitz units should not operate again on the deployment turn.");

        player.StartTurn();
        UnitTurnRules.ReadyForTurn(scout);
        AssertEqual(2, player.Kredits, "Second player turn should refill two Kredits.");
        AssertTrue(UnitActionHighlightRules.ShouldHighlightAdvanceTargets(scout, player.Kredits, false, PlayerSide.Player), "Ready support unit should be able to advance on the next turn.");
        AssertTrue(player.TrySpendKredits(scout.OperationCost), "Advancing should spend operation Kredits.");
        scout.Zone = CardZone.Frontline;
        scout.HasActed = true;
        AssertEqual(1, player.Kredits, "Advancing should leave remaining Kredits for later actions.");

        player.StartTurn();
        UnitTurnRules.ReadyForTurn(scout);
        AssertEqual(3, player.Kredits, "Third player turn should refill three Kredits.");
        AssertTrue(UnitActionHighlightRules.ShouldHighlightAttackTargets(scout, player.Kredits), "Ready frontline unit should be able to attack.");
        AssertTrue(player.TrySpendKredits(scout.OperationCost), "Attacking should spend operation Kredits.");
        UnitAttackRules.MarkAttackResolved(scout);
        PlayerState enemy = new PlayerState(PlayerSide.Enemy);
        enemy.HeadquartersHealth -= scout.Attack;
        AssertTrue(BoardTargetRules.IsHeadquartersSlot(BoardTargetRules.HeadquartersSlotIndex), "HQ target should be represented as a click-targetable pseudo-slot.");
        AssertEqual(19, enemy.HeadquartersHealth, "Attacking the enemy headquarters should damage the HQ card.");
        AssertTrue(scout.HasActed, "Ordinary units should be spent after attacking once.");
        AssertTrue(!UnitActionHighlightRules.ShouldHighlightAttackTargets(scout, player.Kredits), "Spent attackers should not remain highlighted.");
        return 0;
    }

    private static RuntimeCard Unit(CardZone zone, int cost, int operationCost)
    {
        return new RuntimeCard
        {
            CardName = "Forward Scouts",
            Type = CardType.Unit,
            Owner = PlayerSide.Player,
            Zone = zone,
            KreditCost = cost,
            OperationCost = operationCost,
            Attack = 1,
            Defense = 2,
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

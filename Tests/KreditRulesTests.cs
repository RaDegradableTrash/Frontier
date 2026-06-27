using System;

public static class KreditRulesTests
{
    public static int Main()
    {
        AssertEqual(1, KreditRules.NextMaxKredits(0), "First turn should grant one max Kredit.");
        AssertEqual(12, KreditRules.NextMaxKredits(12), "Max Kredits should cap at twelve.");
        AssertTrue(KreditRules.CanSpend(2, 2), "Players should be able to spend exact available Kredits.");
        AssertTrue(!KreditRules.CanSpend(1, 2), "Players should not spend more Kredits than available.");
        AssertTrue(!KreditRules.CanSpend(2, -1), "Negative Kredit costs should be rejected.");

        PlayerState player = new PlayerState(PlayerSide.Player);
        player.StartTurn();
        AssertEqual(1, player.MaxKredits, "PlayerState should use KreditRules for max Kredit growth.");
        AssertEqual(1, player.Kredits, "PlayerState should refill current Kredits at turn start.");
        AssertTrue(player.TrySpendKredits(1), "Affordable costs should be spendable.");
        AssertEqual(0, player.Kredits, "Spending should subtract Kredits exactly.");
        AssertTrue(!player.TrySpendKredits(1), "Unaffordable costs should not be spendable.");
        AssertEqual(0, player.Kredits, "Failed spending should not make Kredits negative.");
        return 0;
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

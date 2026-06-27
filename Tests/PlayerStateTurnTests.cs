using System;

public static class PlayerStateTurnTests
{
    public static int Main()
    {
        PlayerState player = new PlayerState(PlayerSide.Player);

        player.StartTurn();
        AssertTrue(player.MaxKredits == 1, "First turn should grant one max Kredit.");
        AssertTrue(player.Kredits == 1, "StartTurn should refill Kredits to max.");

        player.Kredits = 0;
        player.StartTurn();
        AssertTrue(player.MaxKredits == 2, "Each new turn should increase max Kredits by one.");
        AssertTrue(player.Kredits == 2, "Each new turn should refill all Kredits.");

        for (int i = 0; i < 20; i++)
        {
            player.StartTurn();
        }

        AssertTrue(player.MaxKredits == 12, "Max Kredits should cap at 12.");
        AssertTrue(player.Kredits == 12, "Kredits should refill to the capped max.");
        return 0;
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }
}

using System;

public static class KreditDisplayTextRulesTests
{
    public static int Main()
    {
        PlayerState player = new PlayerState(PlayerSide.Player)
        {
            Kredits = 3,
            MaxKredits = 5
        };

        string text = KreditDisplayTextRules.Build(player);
        AssertEqual("K\n3/5", text, "Kredit display should be a compact Kards-like label plus current/max value.");
        AssertTrue(!text.Contains("TURN"), "Kredit display should not duplicate turn instructions.");
        AssertTrue(!text.Contains("PLAY"), "Kredit display should not write rules on the table.");

        AssertEqual("K\n0/0", KreditDisplayTextRules.Build(null), "Null display state should stay compact and safe.");
        return 0;
    }

    private static void AssertEqual(string expected, string actual, string message)
    {
        if (expected != actual)
        {
            throw new Exception($"{message} Expected '{expected}', got '{actual}'.");
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

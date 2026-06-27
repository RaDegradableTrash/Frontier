using System;

public static class HeadquartersDisplayTextRulesTests
{
    public static int Main()
    {
        AssertEqual("20", HeadquartersDisplayTextRules.Health(20), "Full headquarters health should show the current value.");
        AssertEqual("7", HeadquartersDisplayTextRules.Health(7), "Damaged headquarters health should show the damaged value.");
        AssertEqual("0", HeadquartersDisplayTextRules.Health(-3), "Destroyed headquarters health should clamp visually at zero.");
        return 0;
    }

    private static void AssertEqual(string expected, string actual, string message)
    {
        if (expected != actual)
        {
            throw new Exception($"{message} Expected '{expected}', got '{actual}'.");
        }
    }
}

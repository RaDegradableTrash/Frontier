using System;

public static class DragTargetLabelRulesTests
{
    public static int Main()
    {
        RuntimeCard supportUnit = new RuntimeCard { Type = CardType.Unit, Zone = CardZone.PlayerSupport };
        RuntimeCard frontlineUnit = new RuntimeCard { Type = CardType.Unit, Zone = CardZone.Frontline };

        AssertEqual("ADVANCE", DragTargetLabelRules.LabelFor(supportUnit, SlotZone.Frontline, false), "Support unit drags to the frontline should label movement as ADVANCE.");
        AssertEqual("MOVE", DragTargetLabelRules.LabelFor(supportUnit, SlotZone.PlayerSupport, false), "Support unit drags outside frontline should not look like an attack.");
        AssertEqual("ATTACK", DragTargetLabelRules.LabelFor(frontlineUnit, SlotZone.EnemySupport, true), "Frontline unit legal target drags should label attacks.");
        AssertEqual("TARGET", DragTargetLabelRules.LabelFor(frontlineUnit, SlotZone.PlayerSupport, false), "Illegal frontline drags should remain only target previews.");
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

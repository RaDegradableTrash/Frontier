using System.Collections.Generic;
using System.Text;

public static class StatusSnapshotTextRules
{
    public static string Build(
        PlayerState player,
        PlayerState enemy,
        GamePhase phase,
        PlayerSide activeSide,
        string frontline,
        string status,
        IList<string> actionLog,
        int maxLogLines)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"YOU HQ {player.HeadquartersHealth}   ENEMY HQ {enemy.HeadquartersHealth}");
        builder.AppendLine($"KREDIT {player.Kredits}/{player.MaxKredits}   TURN {ReadableSide(activeSide)}   FRONT {ReadableFrontline(frontline)}");
        if (!string.IsNullOrEmpty(status))
        {
            builder.AppendLine(status);
        }

        return builder.ToString();
    }

    private static string ReadableSide(PlayerSide side)
    {
        return side == PlayerSide.Player ? "YOU" : "ENEMY";
    }

    private static string ReadableFrontline(string frontline)
    {
        if (string.IsNullOrEmpty(frontline) || frontline == "Neutral")
        {
            return "NEUTRAL";
        }

        return frontline == "Player" ? "YOU" : frontline.ToUpperInvariant();
    }

}

public static class SceneCommandRules
{
    public static bool IsAvailable(
        SceneCommandType command,
        GamePhase phase,
        PlayerSide activeSide,
        bool isResolvingEvents,
        bool hasBoard,
        bool hasValidDeck,
        bool mulliganUsed)
    {
        if (isResolvingEvents && command != SceneCommandType.Restart)
        {
            return false;
        }

        switch (command)
        {
            case SceneCommandType.StartMatch:
                return phase == GamePhase.DeckBuilder && hasValidDeck;
            case SceneCommandType.KeepHand:
                return phase == GamePhase.Mulligan;
            case SceneCommandType.Mulligan:
                return phase == GamePhase.Mulligan && !mulliganUsed;
            case SceneCommandType.EndTurn:
                return phase == GamePhase.PlayerTurn && activeSide == PlayerSide.Player;
            case SceneCommandType.Restart:
                return phase == GamePhase.GameOver
                    || phase == GamePhase.PlayerTurn
                    || phase == GamePhase.EnemyTurn
                    || phase == GamePhase.Mulligan;
            case SceneCommandType.StrikeBoard:
                return phase == GamePhase.PlayerTurn && activeSide == PlayerSide.Player && hasBoard;
            case SceneCommandType.SelectAlliedTempo:
            case SceneCommandType.SelectAxisArmor:
            case SceneCommandType.SelectSovietControl:
            case SceneCommandType.SelectJapanAmbush:
                return phase == GamePhase.DeckBuilder;
            default:
                return false;
        }
    }

    public static bool IsVisible(SceneCommandType command, GamePhase phase, bool available, bool showDebugStrike)
    {
        if (command == SceneCommandType.StrikeBoard && !showDebugStrike)
        {
            return false;
        }

        if (available)
        {
            return true;
        }

        switch (phase)
        {
            case GamePhase.DeckBuilder:
                return command == SceneCommandType.StartMatch
                    || command == SceneCommandType.SelectAlliedTempo
                    || command == SceneCommandType.SelectAxisArmor
                    || command == SceneCommandType.SelectSovietControl
                    || command == SceneCommandType.SelectJapanAmbush;
            case GamePhase.Mulligan:
                return command == SceneCommandType.KeepHand
                    || command == SceneCommandType.Mulligan
                    || command == SceneCommandType.Restart;
            case GamePhase.PlayerTurn:
                return command == SceneCommandType.EndTurn
                    || command == SceneCommandType.Restart
                    || (showDebugStrike && command == SceneCommandType.StrikeBoard);
            case GamePhase.GameOver:
                return command == SceneCommandType.Restart;
            default:
                return false;
        }
    }

    public static bool ShouldForwardVisibleClick(bool visible)
    {
        return visible;
    }
}

using System.Collections.Generic;

public class PlayerState
{
    public PlayerSide Side;
    public int HeadquartersHealth = 20;
    public int MaxKredits;
    public int Kredits;
    public readonly List<RuntimeCard> Deck = new List<RuntimeCard>();
    public readonly List<RuntimeCard> Hand = new List<RuntimeCard>();
    public readonly List<RuntimeCard> Discard = new List<RuntimeCard>();
    public readonly List<RuntimeCard> Countermeasures = new List<RuntimeCard>();

    public PlayerState(PlayerSide side)
    {
        Side = side;
    }

    public void StartTurn()
    {
        MaxKredits = KreditRules.NextMaxKredits(MaxKredits);
        Kredits = KreditRules.RefilledKredits(MaxKredits);
    }

    public bool CanSpendKredits(int cost)
    {
        return KreditRules.CanSpend(Kredits, cost);
    }

    public bool TrySpendKredits(int cost)
    {
        if (!CanSpendKredits(cost))
        {
            return false;
        }

        Kredits = KreditRules.Spend(Kredits, cost);
        return true;
    }
}

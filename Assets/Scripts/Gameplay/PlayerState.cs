using UnityEngine;
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
    public int CardsPlayedThisTurn;
    private int pendingFieldIntelCards;
    private int gilbertaAuraSources;

    public PlayerState(PlayerSide side)
    {
        Side = side;
    }

    public int EffectiveDeploymentCost(int baseCost)
    {
        return Mathf.Max(0, baseCost);
    }

    public int EffectiveOperationCost(int baseCost)
    {
        return Mathf.Max(0, baseCost - gilbertaAuraSources);
    }

    public int EffectiveDeploymentCost(RuntimeCard card)
    {
        return card == null ? int.MaxValue : Mathf.Max(0, card.EffectiveDeploymentCost);
    }

    public int EffectiveOperationCost(RuntimeCard card)
    {
        return card == null ? int.MaxValue : Mathf.Max(0, card.EffectiveOperationCost - gilbertaAuraSources);
    }

    public bool CanSpendDeploymentCost(int cost)
    {
        return KreditRules.CanSpend(Kredits, cost);
    }

    public bool TrySpendDeploymentCost(int cost)
    {
        return TrySpendKredits(cost);
    }

    public bool CanSpendOperationCost(RuntimeCard unit)
    {
        return CanSpendOperationCost(EffectiveOperationCost(unit));
    }

    public bool CanSpendOperationCost(int cost)
    {
        return KreditRules.CanSpend(Kredits, cost);
    }

    public bool TrySpendOperationCost(int cost)
    {
        return TrySpendKredits(cost);
    }

    public void RegisterCardPlayed()
    {
        CardsPlayedThisTurn++;
    }

    public void MarkFieldIntelPending(int amount = 1)
    {
        pendingFieldIntelCards += Mathf.Max(1, amount);
    }

    public bool ConsumeFieldIntelDraw()
    {
        if (pendingFieldIntelCards <= 0)
        {
            return false;
        }

        pendingFieldIntelCards--;
        return true;
    }

    public int GilbertaAuraSources => gilbertaAuraSources;

    public void RegisterGilbertaAura()
    {
        gilbertaAuraSources++;
    }

    public void RemoveGilbertaAura()
    {
        if (gilbertaAuraSources <= 0)
        {
            return;
        }

        gilbertaAuraSources--;
    }

    public int PendingFieldIntelCount => pendingFieldIntelCards;

    public void StartTurn()
    {
        MaxKredits = KreditRules.NextMaxKredits(MaxKredits);
        Kredits = KreditRules.RefilledKredits(MaxKredits);
        CardsPlayedThisTurn = 0;
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

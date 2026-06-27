using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Frontier/Card Data")]
public class CardData : ScriptableObject
{
    public int id;
    public string cardName;
    public string nation;
    public CardFaction faction = CardFaction.Britain;
    public CardRarity rarity = CardRarity.Standard;
    public CardType type = CardType.Unit;
    [Min(0)] public int kreditCost = 1;
    [Min(0)] public int operationCost = 1;
    [TextArea] public string description;
    public CardTrigger trigger = CardTrigger.None;
    public CardEffectType effectType = CardEffectType.None;
    public int effectAmount;
    public CardKeyword keywords = CardKeyword.None;
    public Sprite artwork;
    public int attack = 1;
    public int defense = 1;

    public RuntimeCard ToRuntimeCard(PlayerSide owner)
    {
        return new RuntimeCard
        {
            Id = System.Guid.NewGuid().ToString("N"),
            CardName = string.IsNullOrWhiteSpace(cardName) ? name : cardName,
            Nation = nation,
            Faction = faction,
            Rarity = rarity,
            Type = type,
            KreditCost = kreditCost,
            OperationCost = operationCost,
            Attack = attack,
            Defense = defense,
            CurrentDefense = defense,
            RulesText = description,
            Trigger = trigger,
            EffectType = effectType,
            EffectAmount = effectAmount,
            Keywords = keywords,
            Owner = owner,
            Zone = CardZone.Deck
        };
    }
}

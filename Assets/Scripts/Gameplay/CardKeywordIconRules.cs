using System.Collections.Generic;

public static class CardKeywordIconRules
{
    public static readonly CardKeyword[] DisplayOrder =
    {
        CardKeyword.Blitz,
        CardKeyword.Fury,
        CardKeyword.Guard,
        CardKeyword.Smokescreen,
        CardKeyword.Ambush,
        CardKeyword.Mobilize,
        CardKeyword.HeavyArmor,
        CardKeyword.Pinned
    };

    public static List<CardKeyword> VisibleKeywords(RuntimeCard card)
    {
        List<CardKeyword> keywords = new List<CardKeyword>();
        if (card == null)
        {
            return keywords;
        }

        foreach (CardKeyword keyword in DisplayOrder)
        {
            if (card.HasKeyword(keyword))
            {
                keywords.Add(keyword);
            }
        }

        return keywords;
    }

    public static string Label(CardKeyword keyword)
    {
        switch (keyword)
        {
            case CardKeyword.Blitz:
                return "冲击";
            case CardKeyword.Fury:
                return "奋战";
            case CardKeyword.Guard:
                return "守护";
            case CardKeyword.Smokescreen:
                return "烟幕";
            case CardKeyword.Ambush:
                return "伏击";
            case CardKeyword.Mobilize:
                return "动员";
            case CardKeyword.HeavyArmor:
                return "重甲";
            case CardKeyword.Pinned:
                return "压制";
            default:
                return keyword.ToString();
        }
    }

    public static bool ShouldShowOnBoard(RuntimeCard card)
    {
        return card != null
            && card.Type == CardType.Unit
            && card.Zone != CardZone.Hand
            && card.Zone != CardZone.Deck
            && VisibleKeywords(card).Count > 0;
    }
}

public static class DeckAssetRules
{
    public static int TargetDeckSize(int assetCount)
    {
        return assetCount <= 0 ? 0 : DeckRules.MinimumDeckSize;
    }

    public static int TemplateIndexForPosition(int position, int assetCount)
    {
        return assetCount <= 0 ? 0 : position % assetCount;
    }
}

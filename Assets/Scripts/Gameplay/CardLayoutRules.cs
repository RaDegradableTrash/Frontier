public static class CardLayoutRules
{
    public static float OffsetIndex(int index, int count)
    {
        return index - (count - 1) * 0.5f;
    }

    public static float HandFanRotationDegrees(int index, int count)
    {
        return 0f;
    }

    public static float HandFanDepthOffset(int index, int count)
    {
        return 0f;
    }

    public static float HandLayerHeightOffset(int index)
    {
        return index * 0.006f;
    }

    public static int NewlyAddedIndex(int countAfterAdd)
    {
        return countAfterAdd <= 0 ? 0 : countAfterAdd - 1;
    }
}

public static class CardLayoutRules
{
    public static float OffsetIndex(int index, int count)
    {
        return index - (count - 1) * 0.5f;
    }

    public static float HandFanRotationDegrees(int index, int count)
    {
        return OffsetIndex(index, count) * -6.5f;
    }

    public static float HandFanDepthOffset(int index, int count)
    {
        float offset = System.Math.Abs(OffsetIndex(index, count));
        return -0.055f * offset;
    }

    public static int NewlyAddedIndex(int countAfterAdd)
    {
        return countAfterAdd <= 0 ? 0 : countAfterAdd - 1;
    }
}

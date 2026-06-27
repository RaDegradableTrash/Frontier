public static class CardGalleryLayout
{
    public static CardGallerySlot SlotFor(int index, int columns)
    {
        if (columns <= 0)
        {
            return new CardGallerySlot(0, index);
        }

        return new CardGallerySlot(index / columns, index % columns);
    }
}

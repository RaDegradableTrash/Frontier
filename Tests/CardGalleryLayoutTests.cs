using System;

public static class CardGalleryLayoutTests
{
    public static int Main()
    {
        CardGallerySlot first = CardGalleryLayout.SlotFor(0, 3);
        AssertEqual(0, first.Row, "First card should be in row 0.");
        AssertEqual(0, first.Column, "First card should be in column 0.");

        CardGallerySlot fourth = CardGalleryLayout.SlotFor(3, 3);
        AssertEqual(1, fourth.Row, "Fourth card should wrap to row 1.");
        AssertEqual(0, fourth.Column, "Fourth card should restart at column 0.");

        CardGallerySlot invalidColumns = CardGalleryLayout.SlotFor(4, 0);
        AssertEqual(4, invalidColumns.Column, "Invalid column counts should fall back to one row.");
        AssertEqual(0, invalidColumns.Row, "Invalid column counts should not divide rows.");
        return 0;
    }

    private static void AssertEqual(int expected, int actual, string message)
    {
        if (expected != actual)
        {
            throw new Exception($"{message} Expected {expected}, got {actual}.");
        }
    }
}

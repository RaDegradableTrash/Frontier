public static class HeadquartersDisplayTextRules
{
    public static string Health(int health)
    {
        return (health < 0 ? 0 : health).ToString();
    }
}

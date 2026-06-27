public static class KreditDisplayTextRules
{
    public static string Build(PlayerState state)
    {
        if (state == null)
        {
            return "K\n0/0";
        }

        return $"K\n{state.Kredits}/{state.MaxKredits}";
    }
}

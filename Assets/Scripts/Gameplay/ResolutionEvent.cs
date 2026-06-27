using UnityEngine;

public class ResolutionEvent
{
    public readonly string Text;
    public readonly Vector3 Position;
    public readonly Color Color;
    public readonly FeedbackCueType CueType;
    public readonly float Delay;

    public ResolutionEvent(string text, Vector3 position, Color color, FeedbackCueType cueType, float delay = 0.22f)
    {
        Text = text;
        Position = position;
        Color = color;
        CueType = cueType;
        Delay = delay;
    }
}

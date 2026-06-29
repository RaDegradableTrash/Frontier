using UnityEngine;

public class FeedbackManager : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.18f;
    }

    public void PlayCue(FeedbackCueType cueType, Vector3 position)
    {
        PlayTone(FrequencyFor(cueType), DurationFor(cueType));
        SpawnPulse(cueType, position);
    }

    private void PlayTone(float frequency, float duration)
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        AudioClip clip = AudioClip.Create("Cue", sampleCount, 1, sampleRate, false);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Clamp01(1f - t / duration);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.25f;
        }

        clip.SetData(samples, 0);
        audioSource.PlayOneShot(clip);
    }

    private void SpawnPulse(FeedbackCueType cueType, Vector3 position)
    {
        GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pulse.name = $"CuePulse_{cueType}";
        pulse.transform.position = position + Vector3.up * 0.12f;
        pulse.transform.localScale = Vector3.one * SizeFor(cueType);
        RuntimeSafeDestroy.Destroy(pulse.GetComponent<Collider>());

        MeshRenderer renderer = pulse.GetComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = ColorFor(cueType);

        CuePulse cuePulse = pulse.AddComponent<CuePulse>();
        cuePulse.Initialize(renderer.material.color, 0.45f);
    }

    private float FrequencyFor(FeedbackCueType cueType)
    {
        switch (cueType)
        {
            case FeedbackCueType.Deploy:
                return 392f;
            case FeedbackCueType.Advance:
                return 494f;
            case FeedbackCueType.Attack:
                return 196f;
            case FeedbackCueType.Damage:
                return 146f;
            case FeedbackCueType.Heal:
                return 660f;
            case FeedbackCueType.Buff:
                return 587f;
            case FeedbackCueType.Countermeasure:
                return 220f;
            case FeedbackCueType.Pin:
                return 330f;
            case FeedbackCueType.Draw:
                return 523f;
            default:
                return 110f;
        }
    }

    private float DurationFor(FeedbackCueType cueType)
    {
        return cueType == FeedbackCueType.Attack || cueType == FeedbackCueType.Countermeasure ? 0.18f : 0.11f;
    }

    private float SizeFor(FeedbackCueType cueType)
    {
        return cueType == FeedbackCueType.Attack || cueType == FeedbackCueType.Damage ? 0.24f : 0.16f;
    }

    private Color ColorFor(FeedbackCueType cueType)
    {
        switch (cueType)
        {
            case FeedbackCueType.Deploy:
                return Color.cyan;
            case FeedbackCueType.Advance:
                return Color.yellow;
            case FeedbackCueType.Attack:
            case FeedbackCueType.Damage:
                return Color.red;
            case FeedbackCueType.Heal:
            case FeedbackCueType.Buff:
                return Color.green;
            case FeedbackCueType.Countermeasure:
                return Color.magenta;
            case FeedbackCueType.Pin:
                return new Color(1f, 0.8f, 0.15f);
            case FeedbackCueType.Draw:
                return new Color(0.55f, 0.75f, 1f);
            default:
                return Color.gray;
        }
    }
}

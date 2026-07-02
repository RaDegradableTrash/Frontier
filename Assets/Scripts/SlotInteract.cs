using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SlotInteract : MonoBehaviour
{
    public int X { get; private set; }
    public int Z { get; private set; }
    public SlotZone Zone { get; private set; }
    public RuntimeCard Occupant { get; private set; }
    public bool IsOccupied => Occupant != null;

    private BoardManager board;
    private SlotVisualize_Temp visual;
    private Vector3 originalLocalPosition;
    private float impulse;
    private float impulseOffset;
    private Vector3 strikeOrigin;

    public void Initialize(BoardManager owner, int x, int z, SlotZone zone, SlotVisualize_Temp slotVisual)
    {
        board = owner;
        X = x;
        Z = z;
        Zone = zone;
        visual = slotVisual;
        originalLocalPosition = transform.localPosition;
    }

    public void SetOccupant(RuntimeCard card)
    {
        Occupant = card;
    }

    public void ClearOccupant(RuntimeCard card)
    {
        if (Occupant == card)
        {
            Occupant = null;
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        visual?.SetHighlighted(highlighted);
    }

    public void SetHighlighted(bool highlighted, string label)
    {
        visual?.SetHighlighted(highlighted, label);
    }

    public void DoStrike(Vector3 originPosition, float force)
    {
        float distance = Vector3.Distance(transform.position, originPosition);
        strikeOrigin = originPosition;
        impulseOffset = distance;
        impulse = force;
        float delay = Mathf.Lerp(0.02f, 0.16f, Mathf.InverseLerp(0f, 5f, distance));
        StopAllCoroutines();
        float strength = force / (1f + distance * 0.68f);
        StartCoroutine(StrikeAnimation(strength, delay));
    }

    private void OnMouseDown()
    {
        board?.HandleSlotClicked(this);
    }

    private IEnumerator StrikeAnimation(float strength, float delay)
    {
        yield return new WaitForSeconds(delay);

        float duration = 0.82f;
        float elapsed = 0f;
        float attenuation = Mathf.Clamp01(1f / (1f + impulseOffset * 0.95f));
        float distanceDecay = Mathf.Clamp01(1f / (1f + impulseOffset * 1.2f));

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float wave = Mathf.Sin(t * Mathf.PI);
            float settle = Mathf.SmoothStep(0f, 1f, t);
            float travel = 1f - Mathf.Cos(Mathf.Clamp01(t) * Mathf.PI * 0.5f);
            float outward = 1f - t * t;
            Vector3 direction = transform.position - strikeOrigin;
            direction.y = 0f;
            Vector3 flatDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;

            float hop = wave * (0.38f * strength * distanceDecay) * Mathf.Max(0.16f, 1f - travel * 0.45f);
            float push = 0.18f * strength * distanceDecay * Mathf.Pow(1f - attenuation, 0.15f);
            Vector3 rippleOffset = flatDirection * push * travel * outward;

            float noise = Mathf.Sin((t * 11f) + originalLocalPosition.x * 12f) + Mathf.Cos((t * 13f) + originalLocalPosition.z * 12f);
            Vector3 jitter = new Vector3(
                noise * 0.0085f * distanceDecay,
                0f,
                -noise * 0.0085f * distanceDecay);

            float spin = 10f * strength * distanceDecay * Mathf.Cos(t * Mathf.PI * 2.1f) * Mathf.Pow(1f - t, 0.72f);
            float tilt = 8f * strength * distanceDecay * Mathf.Sin(t * Mathf.PI * 0.95f) * Mathf.Pow(1f - t, 0.44f);
            float yaw = 5f * strength * (1f - distanceDecay) * Mathf.Cos(t * Mathf.PI * 1.1f) * Mathf.Pow(1f - t, 0.52f);
            float roll = 2.5f * strength * Mathf.Cos(t * Mathf.PI * 0.9f) * Mathf.Pow(1f - t, 0.56f);

            Quaternion localTilt = Quaternion.Euler(
                tilt * flatDirection.z,
                spin,
                yaw + flatDirection.x * tilt * 0.85f + roll);
            Quaternion wobble = Quaternion.Slerp(Quaternion.identity, localTilt, 0.75f);
            transform.localPosition = Vector3.Lerp(originalLocalPosition, originalLocalPosition + rippleOffset + jitter, settle) + Vector3.up * hop;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, wobble, 0.28f + strength * 0.18f);
            yield return null;
        }

        transform.localPosition = originalLocalPosition;
        transform.localRotation = Quaternion.identity;
    }
}

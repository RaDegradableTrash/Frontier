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
    private Vector3 strikeLocalDirection;

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
        Vector3 worldDirection = transform.position - strikeOrigin;
        worldDirection.y = 0f;
        strikeLocalDirection = worldDirection.sqrMagnitude > 0.0001f
            ? transform.parent != null ? transform.parent.InverseTransformDirection(worldDirection.normalized) : worldDirection.normalized
            : Vector3.zero;
        float delay = Mathf.Lerp(0.015f, 0.22f, Mathf.InverseLerp(0f, 6.8f, distance));
        StopAllCoroutines();
        float strength = force / (1f + distance * 0.46f);
        StartCoroutine(StrikeAnimation(strength, delay));
    }

    private void OnMouseDown()
    {
        board?.HandleSlotClicked(this);
    }

    private IEnumerator StrikeAnimation(float strength, float delay)
    {
        yield return new WaitForSeconds(delay);

        float duration = 1.08f;
        float elapsed = 0f;
        float attenuation = Mathf.Clamp01(1f / (1f + impulseOffset * 0.62f));
        float distanceDecay = Mathf.Clamp01(1f / (1f + impulseOffset * 0.72f));

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float wave = Mathf.Sin(t * Mathf.PI);
            float settle = Mathf.SmoothStep(0f, 1f, t);
            float travel = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI * 0.72f);
            float outward = Mathf.Pow(1f - t, 1.35f);
            Vector3 flatDirection = strikeLocalDirection;

            float hop = wave * (0.62f * strength * distanceDecay) * Mathf.Max(0.12f, 1f - t * 0.22f);
            float push = 0.34f * strength * distanceDecay * Mathf.Lerp(1.15f, 0.22f, attenuation);
            Vector3 rippleOffset = flatDirection * push * travel * outward;

            float noise = Mathf.Sin((t * 11f) + originalLocalPosition.x * 12f) + Mathf.Cos((t * 13f) + originalLocalPosition.z * 12f);
            Vector3 jitter = new Vector3(
                noise * 0.014f * distanceDecay,
                0f,
                -noise * 0.014f * distanceDecay);

            float spin = 18f * strength * distanceDecay * Mathf.Cos(t * Mathf.PI * 2.4f) * Mathf.Pow(1f - t, 0.78f);
            float tilt = 15f * strength * distanceDecay * Mathf.Sin(t * Mathf.PI * 0.95f) * Mathf.Pow(1f - t, 0.52f);
            float yaw = 8f * strength * distanceDecay * Mathf.Cos(t * Mathf.PI * 1.35f) * Mathf.Pow(1f - t, 0.56f);
            float roll = 9f * strength * distanceDecay * Mathf.Cos(t * Mathf.PI * 1.1f) * Mathf.Pow(1f - t, 0.62f);

            Quaternion localTilt = Quaternion.Euler(
                tilt * flatDirection.z,
                spin,
                yaw + flatDirection.x * tilt * 0.85f + roll);
            Quaternion wobble = Quaternion.Slerp(Quaternion.identity, localTilt, 0.75f);
            transform.localPosition = originalLocalPosition + Vector3.LerpUnclamped(rippleOffset + jitter, Vector3.zero, settle) + Vector3.up * hop;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, wobble, 0.34f + strength * 0.2f);
            yield return null;
        }

        transform.localPosition = originalLocalPosition;
        transform.localRotation = Quaternion.identity;
    }
}

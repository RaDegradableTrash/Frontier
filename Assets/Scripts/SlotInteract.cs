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
        float delay = distance * 0.05f;
        StopAllCoroutines();
        StartCoroutine(StrikeAnimation(force / (distance + 1f), delay));
    }

    private void OnMouseDown()
    {
        board?.HandleSlotClicked(this);
    }

    private IEnumerator StrikeAnimation(float strength, float delay)
    {
        yield return new WaitForSeconds(delay);

        const float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float offset = Mathf.Sin(t * Mathf.PI) * strength;
            transform.localPosition = originalLocalPosition + Vector3.up * offset;
            yield return null;
        }

        transform.localPosition = originalLocalPosition;
    }
}

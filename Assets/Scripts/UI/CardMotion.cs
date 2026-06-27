using UnityEngine;

public class CardMotion : MonoBehaviour
{
    private Vector3 baseScale;
    private Vector3 targetPosition;
    private float spawnElapsed;
    private float pulse;
    private float lungeElapsed;
    private bool hovered;
    private bool selected;
    private bool hasTargetPosition;
    private bool dragging;
    private Vector3 lungeOffset;

    private void Awake()
    {
        baseScale = transform.localScale;
        targetPosition = transform.position;
        transform.localScale = baseScale * CardMotionRules.SpawnScaleMultiplier;
    }

    public void SetHovered(bool value)
    {
        hovered = value;
    }

    public void SetSelected(bool value)
    {
        selected = value;
        pulse = value ? 1f : 0f;
    }

    public void ResetBasePosition(Vector3 position)
    {
        targetPosition = position;
        hasTargetPosition = true;
    }

    public void SetDragging(bool value)
    {
        dragging = value;
    }

    public void SetBaseScale(Vector3 scale)
    {
        baseScale = scale;
    }

    public void PlayAttackLunge(Vector3 target)
    {
        if (!CardMotionRules.ShouldApplyLunge(dragging, hasTargetPosition))
        {
            return;
        }

        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        lungeOffset = direction.normalized * Mathf.Min(direction.magnitude * CardMotionRules.AttackLungeDistanceRatio, 0.42f);
        lungeElapsed = CardMotionRules.AttackLungeReturnSeconds;
    }

    private void Update()
    {
        spawnElapsed += Time.deltaTime;
        float spawnT = Mathf.Clamp01(spawnElapsed / 0.18f);
        float selectedScale = selected ? 1.08f + Mathf.Sin(Time.time * 8f) * 0.025f : 1f;
        float hoverScale = hovered ? 1.06f : 1f;
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * selectedScale * hoverScale, Time.deltaTime * CardMotionRules.ScaleLerpSpeed + spawnT * 0.05f);

        if (!selected && pulse > 0f)
        {
            pulse = Mathf.Max(0f, pulse - Time.deltaTime * 4f);
        }

        if (CardMotionRules.ShouldAnimatePosition(dragging, hasTargetPosition))
        {
            float distance = Vector3.Distance(transform.position, targetPosition);
            if (CardMotionRules.ShouldSnapToTarget(distance))
            {
                transform.position = targetPosition;
                hasTargetPosition = false;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * CardMotionRules.MoveLerpSpeed);
            }
        }

        if (lungeElapsed > 0f)
        {
            lungeElapsed = Mathf.Max(0f, lungeElapsed - Time.deltaTime);
            if (lungeElapsed <= 0f)
            {
                transform.position = targetPosition;
                return;
            }

            float t = lungeElapsed / CardMotionRules.AttackLungeReturnSeconds;
            Vector3 lungeTarget = targetPosition + lungeOffset * Mathf.Sin(t * Mathf.PI);
            transform.position = Vector3.Lerp(transform.position, lungeTarget, Time.deltaTime * CardMotionRules.MoveLerpSpeed);
        }
    }
}

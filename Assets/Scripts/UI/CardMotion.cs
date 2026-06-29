using UnityEngine;

public class CardMotion : MonoBehaviour
{
    private Vector3 baseScale;
    private Vector3 targetPosition;
    private float spawnElapsed;
    private float pulse;
    private float lungeElapsed;
    private float specialMoveElapsed;
    private float specialMoveDuration;
    private Vector3 specialMoveStart;
    private Vector3 specialMoveEnd;
    private Quaternion specialMoveStartRotation;
    private Quaternion specialMoveEndRotation;
    private bool hovered;
    private bool selected;
    private bool hasTargetPosition;
    private bool dragging;
    private bool specialMoveActive;
    private bool specialMoveFlips;
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

    public void PlayDeployDrop(Vector3 fromPosition, Vector3 toPosition)
    {
        specialMoveStart = fromPosition;
        specialMoveEnd = toPosition;
        specialMoveStartRotation = transform.rotation;
        specialMoveEndRotation = transform.rotation;
        specialMoveDuration = CardMotionRules.DeployDropSeconds;
        specialMoveElapsed = 0f;
        specialMoveActive = true;
        specialMoveFlips = false;
        hasTargetPosition = false;
        transform.position = fromPosition;
    }

    public void PlayDrawFlight(Vector3 fromPosition, Vector3 toPosition)
    {
        specialMoveStart = fromPosition;
        specialMoveEnd = toPosition;
        specialMoveEndRotation = transform.rotation;
        specialMoveStartRotation = specialMoveEndRotation * Quaternion.Euler(180f, 0f, 0f);
        specialMoveDuration = CardMotionRules.DrawFlightSeconds;
        specialMoveElapsed = 0f;
        specialMoveActive = true;
        specialMoveFlips = true;
        hasTargetPosition = false;
        transform.position = fromPosition;
        transform.rotation = specialMoveStartRotation;
    }

    public void PlayMulliganDiscardFlight(Vector3 fromPosition, Vector3 toPosition)
    {
        specialMoveStart = fromPosition;
        specialMoveEnd = toPosition;
        specialMoveStartRotation = transform.rotation;
        specialMoveEndRotation = transform.rotation;
        specialMoveDuration = CardMotionRules.MulliganDiscardFlightSeconds;
        specialMoveElapsed = 0f;
        specialMoveActive = true;
        specialMoveFlips = false;
        hasTargetPosition = false;
        transform.position = fromPosition;
    }

    private void Update()
    {
        spawnElapsed += Time.deltaTime;
        float spawnT = Mathf.Clamp01(spawnElapsed / 0.18f);
        float selectedScale = selected ? CardMotionRules.SelectedScaleMultiplier : 1f;
        float hoverScale = hovered ? CardMotionRules.HoverScaleMultiplier : 1f;
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            baseScale * selectedScale * hoverScale,
            Time.deltaTime * CardMotionRules.ScaleLerpSpeed + spawnT * 0.05f);

        if (!selected && pulse > 0f)
        {
            pulse = Mathf.Max(0f, pulse - Time.deltaTime * 4f);
        }

        if (specialMoveActive)
        {
            specialMoveElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(specialMoveElapsed / specialMoveDuration);
            float arc = Mathf.Sin(t * Mathf.PI) * 0.28f;
            Vector3 flat = Vector3.Lerp(specialMoveStart, specialMoveEnd, t);
            flat.y = Mathf.Lerp(specialMoveStart.y, specialMoveEnd.y, t) + arc;
            transform.position = flat;
            if (specialMoveFlips)
            {
                transform.rotation = Quaternion.Slerp(specialMoveStartRotation, specialMoveEndRotation, t);
            }

            if (t >= 1f)
            {
                specialMoveActive = false;
                specialMoveFlips = false;
                transform.rotation = specialMoveEndRotation;
                ResetBasePosition(specialMoveEnd);
            }

            return;
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

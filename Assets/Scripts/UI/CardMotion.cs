using UnityEngine;

public class CardMotion : MonoBehaviour
{
    private enum SpecialMoveKind
    {
        None,
        Deploy,
        Draw,
        Discard
    }

    private Vector3 baseScale;
    private Vector3 targetPosition;
    private float spawnElapsed;
    private float pulse;
    private float lungeElapsed;
    private float specialMoveElapsed;
    private float specialMoveDuration;
    private Vector3 specialMoveStart;
    private Vector3 specialMoveMid;
    private Vector3 specialMoveEnd;
    private Quaternion specialMoveStartRotation;
    private Quaternion specialMoveEndRotation;
    private Vector3 returnStart;
    private Vector3 returnEnd;
    private float returnElapsed;
    private float returnDuration;
    private bool hovered;
    private bool selected;
    private bool hasTargetPosition;
    private bool dragging;
    private bool specialMoveActive;
    private bool failedReturnActive;
    private bool specialMoveFlips;
    private bool specialMoveHasMid;
    private SpecialMoveKind specialMoveKind;
    private Vector3 lungeOffset;

    public bool IsSpecialMoveActive => specialMoveActive;

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

    public void Pulse()
    {
        pulse = 1f;
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

    public void BeginManualDrag(Vector3 currentPosition)
    {
        specialMoveActive = false;
        specialMoveFlips = false;
        specialMoveHasMid = false;
        specialMoveKind = SpecialMoveKind.None;
        lungeElapsed = 0f;
        lungeOffset = Vector3.zero;
        targetPosition = currentPosition;
        hasTargetPosition = false;
        dragging = true;
        transform.position = currentPosition;
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
        specialMoveHasMid = false;
        specialMoveKind = SpecialMoveKind.Deploy;
        hasTargetPosition = false;
        transform.position = fromPosition;
    }

    public void PlayDrawFlight(Vector3 fromPosition, Vector3 toPosition)
    {
        PlayDrawFlight(fromPosition, toPosition + Vector3.up * 0.08f, toPosition);
    }

    public void PlayDrawFlight(Vector3 fromPosition, Vector3 stagedPosition, Vector3 toPosition)
    {
        specialMoveStart = fromPosition;
        specialMoveMid = stagedPosition;
        specialMoveEnd = toPosition;
        specialMoveEndRotation = transform.rotation;
        specialMoveStartRotation = specialMoveEndRotation * Quaternion.Euler(180f, 0f, 0f);
        specialMoveDuration = CardMotionRules.DrawFlightSeconds * 1.35f;
        specialMoveElapsed = 0f;
        specialMoveActive = true;
        specialMoveFlips = true;
        specialMoveHasMid = true;
        specialMoveKind = SpecialMoveKind.Draw;
        hasTargetPosition = false;
        transform.position = fromPosition;
        transform.rotation = specialMoveStartRotation;
    }

    public void PlayMulliganDiscardFlight(Vector3 fromPosition, Vector3 toPosition)
    {
        specialMoveStart = fromPosition;
        Vector3 discardDirection = toPosition - fromPosition;
        discardDirection.y = 0f;
        if (discardDirection.sqrMagnitude <= 0.001f)
        {
            discardDirection = Vector3.forward;
        }

        specialMoveMid = toPosition + Vector3.up * 0.08f - discardDirection.normalized * CardMotionRules.DiscardTuckDistance;
        specialMoveEnd = toPosition;
        specialMoveStartRotation = transform.rotation;
        specialMoveEndRotation = transform.rotation * Quaternion.Euler(180f, 0f, 0f);
        specialMoveDuration = CardMotionRules.MulliganDiscardFlightSeconds;
        specialMoveElapsed = 0f;
        specialMoveActive = true;
        specialMoveFlips = true;
        specialMoveHasMid = true;
        specialMoveKind = SpecialMoveKind.Discard;
        hasTargetPosition = false;
        transform.position = fromPosition;
    }

    public void PlayFailedReturn(Vector3 fromPosition, Vector3 toPosition)
    {
        returnStart = fromPosition;
        returnEnd = toPosition;
        returnStart.y += CardMotionRules.FailedReturnSettleHeight;
        returnEnd = toPosition;
        returnDuration = CardMotionRules.FailedReturnSeconds;
        returnElapsed = 0f;
        failedReturnActive = true;
        specialMoveActive = false;
        specialMoveFlips = false;
        specialMoveHasMid = false;
        specialMoveKind = SpecialMoveKind.None;
        hasTargetPosition = false;
        lungeElapsed = 0f;
        lungeOffset = Vector3.zero;
        dragging = false;
        transform.position = fromPosition;
    }

    private void Update()
    {
        if (!specialMoveActive
            && !dragging
            && !hovered
            && !selected
            && pulse <= 0f
            && !CardMotionRules.ShouldAnimatePosition(dragging, hasTargetPosition)
            && lungeElapsed <= 0f)
        {
            if (spawnElapsed < 0.18f)
            {
                spawnElapsed = 0.18f;
                transform.localScale = baseScale;
            }

            return;
        }

        spawnElapsed += Time.deltaTime;
        float spawnT = Mathf.Clamp01(spawnElapsed / 0.18f);
        float selectedScale = selected ? CardMotionRules.SelectedScaleMultiplier : 1f;
        float hoverScale = hovered ? CardMotionRules.HoverScaleMultiplier : 1f;
        float hoverLift = hovered ? CardMotionRules.HoverLift : 0f;
        float pulseScale = 1f + Mathf.Sin(pulse * Mathf.PI) * 0.08f;
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            baseScale * selectedScale * hoverScale * pulseScale,
            Time.deltaTime * CardMotionRules.ScaleLerpSpeed + spawnT * 0.05f);

        if (!selected && pulse > 0f)
        {
            pulse = Mathf.Max(0f, pulse - Time.deltaTime * 4f);
        }

        if (failedReturnActive)
        {
            returnElapsed += Time.deltaTime;
            float linearT = Mathf.Clamp01(returnElapsed / Mathf.Max(0.01f, returnDuration));
            float liftTime = Mathf.Max(0.01f, CardMotionRules.FailedReturnSettleSeconds / returnDuration);
            float settleTime = Mathf.Max(0.01f, 1f - liftTime);
            float hop;
            Vector3 path;
            if (linearT <= liftTime)
            {
                float upT = linearT / liftTime;
                float rise = Mathf.Sin(upT * Mathf.PI * 0.5f);
                hop = rise * CardMotionRules.FailedReturnSettleHeight;
                path = Vector3.LerpUnclamped(returnStart, returnEnd, SmoothEase(upT) * 0.32f);
            }
            else
            {
                float settleT = (linearT - liftTime) / settleTime;
                float cruise = 1f - Mathf.Pow(1f - settleT, 2.6f);
                float settle = SmoothEase(settleT);
                hop = Mathf.Lerp(CardMotionRules.FailedReturnSettleHeight, 0f, settle);
                path = Vector3.LerpUnclamped(returnStart, returnEnd, 0.32f + cruise * 0.68f);
            }

            path.y += hop + Mathf.Sin(Mathf.PI * linearT) * CardMotionRules.FailedReturnHopHeight;
            transform.position = path;
            if (linearT >= 1f)
            {
                failedReturnActive = false;
                ResetBasePosition(returnEnd);
                transform.position = returnEnd;
                return;
            }

            return;
        }

        if (specialMoveActive)
        {
            specialMoveElapsed += Time.deltaTime;
            float linearT = Mathf.Clamp01(specialMoveElapsed / specialMoveDuration);
            transform.position = SpecialMovePosition(linearT);
            if (specialMoveFlips)
            {
                float flipT = specialMoveKind == SpecialMoveKind.Discard
                    ? VariableSpeedEase(linearT)
                    : SmoothEase(Mathf.InverseLerp(CardMotionRules.DrawExtractSecondsRatio * 0.45f, 1f, linearT));
                transform.rotation = Quaternion.Slerp(specialMoveStartRotation, specialMoveEndRotation, flipT);
            }

            if (linearT >= 1f)
            {
                specialMoveActive = false;
                specialMoveFlips = false;
                specialMoveHasMid = false;
                specialMoveKind = SpecialMoveKind.None;
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
                transform.position = targetPosition + Vector3.up * hoverLift;
                hasTargetPosition = false;
            }
            else
            {
                float t = 1f - Mathf.Exp(-CardMotionRules.MoveLerpSpeed * Time.deltaTime);
                transform.position = Vector3.LerpUnclamped(transform.position, targetPosition + Vector3.up * hoverLift, t);
            }
        }

        if (lungeElapsed > 0f)
        {
            lungeElapsed = Mathf.Max(0f, lungeElapsed - Time.deltaTime);
            if (lungeElapsed <= 0f)
            {
                transform.position = targetPosition + Vector3.up * hoverLift;
                return;
            }

            float t = lungeElapsed / CardMotionRules.AttackLungeReturnSeconds;
            Vector3 lungeTarget = targetPosition + lungeOffset * Mathf.Sin(t * Mathf.PI);
            float lungeT = 1f - Mathf.Exp(-CardMotionRules.MoveLerpSpeed * Time.deltaTime);
            transform.position = Vector3.LerpUnclamped(transform.position, lungeTarget + Vector3.up * hoverLift, lungeT);
        }
    }

    private static float DeployImpactEase(float t)
    {
        t = Mathf.Clamp01(t);
        if (t < 0.72f)
        {
            float lead = t / 0.72f;
            return Mathf.SmoothStep(0f, 0.94f, lead);
        }

        float settle = (t - 0.72f) / 0.28f;
        return Mathf.Lerp(1.08f, 1f, Mathf.SmoothStep(0f, 1f, settle));
    }

    private static float SmoothEase(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private Vector3 SpecialMovePosition(float linearT)
    {
        switch (specialMoveKind)
        {
            case SpecialMoveKind.Draw:
                return DrawMovePosition(linearT);
            case SpecialMoveKind.Discard:
                return DiscardMovePosition(linearT);
            case SpecialMoveKind.Deploy:
            default:
                float t = specialMoveFlips ? SmoothEase(linearT) : DeployImpactEase(linearT);
                Vector3 flat = specialMoveHasMid
                    ? TwoPartPosition(linearT, 0.68f, SmoothEase)
                    : Vector3.LerpUnclamped(specialMoveStart, specialMoveEnd, t);
                flat.y += Mathf.Sin(linearT * Mathf.PI) * (specialMoveFlips ? 0.28f : 0.42f);
                return flat;
        }
    }

    private Vector3 DrawMovePosition(float linearT)
    {
        float extractRatio = Mathf.Clamp01(CardMotionRules.DrawExtractSecondsRatio);
        float settleRatio = Mathf.Clamp01(CardMotionRules.DrawSettleSecondsRatio);
        float flightEnd = Mathf.Clamp01(1f - settleRatio);
        Vector3 extractEnd = specialMoveStart + Vector3.up * CardMotionRules.DrawExtractHeight;
        Vector3 settleStart = specialMoveMid + Vector3.up * CardMotionRules.DrawSettleLift;

        if (linearT < extractRatio)
        {
            float t = SmoothEase(linearT / Mathf.Max(0.01f, extractRatio));
            return Vector3.LerpUnclamped(specialMoveStart, extractEnd, t);
        }

        if (linearT < flightEnd)
        {
            float t = VariableSpeedEase((linearT - extractRatio) / Mathf.Max(0.01f, flightEnd - extractRatio));
            Vector3 control = (extractEnd + settleStart) * 0.5f + Vector3.up * CardMotionRules.DrawFlightArcHeight;
            return QuadraticBezier(extractEnd, control, settleStart, t);
        }

        float settleT = SmoothEase((linearT - flightEnd) / Mathf.Max(0.01f, settleRatio));
        Vector3 firstSettle = Vector3.LerpUnclamped(settleStart, specialMoveMid, Mathf.Clamp01(settleT * 1.35f));
        Vector3 secondSettle = Vector3.LerpUnclamped(specialMoveMid, specialMoveEnd, Mathf.Clamp01((settleT - 0.35f) / 0.65f));
        return settleT < 0.35f ? firstSettle : secondSettle;
    }

    private Vector3 DiscardMovePosition(float linearT)
    {
        const float tuckStart = 0.78f;
        if (linearT < tuckStart)
        {
            float t = VariableSpeedEase(linearT / tuckStart);
            Vector3 control = (specialMoveStart + specialMoveMid) * 0.5f + Vector3.up * CardMotionRules.DiscardFlightArcHeight;
            return QuadraticBezier(specialMoveStart, control, specialMoveMid, t);
        }

        float tuckT = SmoothEase((linearT - tuckStart) / (1f - tuckStart));
        return Vector3.LerpUnclamped(specialMoveMid, specialMoveEnd, tuckT);
    }

    private Vector3 TwoPartPosition(float linearT, float split, System.Func<float, float> ease)
    {
        if (linearT < split)
        {
            return Vector3.LerpUnclamped(specialMoveStart, specialMoveMid, ease(linearT / split));
        }

        return Vector3.LerpUnclamped(specialMoveMid, specialMoveEnd, ease((linearT - split) / (1f - split)));
    }

    private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * a + 2f * oneMinusT * t * b + t * t * c;
    }

    private static float VariableSpeedEase(float t)
    {
        t = Mathf.Clamp01(t);
        return t < 0.55f
            ? 0.62f * Mathf.SmoothStep(0f, 1f, t / 0.55f)
            : Mathf.Lerp(0.62f, 1f, 1f - Mathf.Pow(1f - ((t - 0.55f) / 0.45f), 2.8f));
    }
}

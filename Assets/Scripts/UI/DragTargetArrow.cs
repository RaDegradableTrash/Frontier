using UnityEngine;

public class DragTargetArrow : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private LineRenderer shadowRenderer;
    private LineRenderer leftHeadRenderer;
    private LineRenderer rightHeadRenderer;
    private LineRenderer leftHeadShadowRenderer;
    private LineRenderer rightHeadShadowRenderer;
    private TextMesh label;
    private TextMesh targetDamageLabel;
    private TextMesh counterDamageLabel;
    private GameObject targetSkullObject;
    private GameObject counterSkullObject;
    private MeshRenderer targetSkullRenderer;
    private MeshRenderer counterSkullRenderer;

    public void Initialize()
    {
        shadowRenderer = gameObject.AddComponent<LineRenderer>();
        ConfigureRenderer(shadowRenderer, 0.090f, 0.060f, new Color(0.02f, 0.025f, 0.03f, 0.78f), new Color(0.02f, 0.025f, 0.03f, 0.78f));
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        ConfigureRenderer(lineRenderer, 0.055f, 0.040f, new Color(0.96f, 0.98f, 1f, 0.98f), new Color(0.78f, 0.88f, 1f, 0.98f));
        leftHeadShadowRenderer = CreateArrowHeadRenderer("Arrow Head Shadow Left", true);
        rightHeadShadowRenderer = CreateArrowHeadRenderer("Arrow Head Shadow Right", true);
        leftHeadRenderer = CreateArrowHeadRenderer("Arrow Head Left", false);
        rightHeadRenderer = CreateArrowHeadRenderer("Arrow Head Right", false);

        label = CreateLabel("Arrow Label", 0.035f, new Color(0.92f, 0.96f, 1f, 1f));
        targetDamageLabel = CreateLabel("Target Damage Label", 0.048f, new Color(1f, 0.35f, 0.25f, 1f));
        counterDamageLabel = CreateLabel("Counter Damage Label", 0.048f, new Color(1f, 0.35f, 0.25f, 1f));
        targetSkullObject = CreateSkullIcon("Target Skull");
        counterSkullObject = CreateSkullIcon("Counter Skull");
        targetSkullRenderer = targetSkullObject.GetComponent<MeshRenderer>();
        counterSkullRenderer = counterSkullObject.GetComponent<MeshRenderer>();
    }

    public void UpdateArrow(Vector3 start, Vector3 end, string text)
    {
        UpdateArrow(start, end, text, default, default);
    }

    public void UpdateArrow(Vector3 start, Vector3 end, string text, DamagePreview preview, Vector3 counterAnchor)
    {
        if (lineRenderer == null)
        {
            Initialize();
        }

        Vector3 raisedStart = start + Vector3.up * 0.34f;
        Vector3 raisedEnd = end + Vector3.up * 0.34f;
        UpdateArrowGeometry(raisedStart, raisedEnd);

        if (label != null)
        {
            if (preview.IsCanceled)
            {
                label.text = "CANCEL";
            }
            else
            {
                label.text = BuildArrowLabelText(text, preview);
            }

            label.transform.position = Vector3.Lerp(raisedStart, raisedEnd, 0.62f) + Vector3.up * 0.03f;
            label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        HideDamageOverlay(targetDamageLabel, targetSkullObject);
        HideDamageOverlay(counterDamageLabel, counterSkullObject);
    }

    private string BuildArrowLabelText(string text, DamagePreview preview)
    {
        if (preview.AdjacentTargets <= 0)
        {
            return text;
        }

        return string.IsNullOrEmpty(text) ? "AOE" : $"{text} AOE";
    }

    private LineRenderer CreateArrowHeadRenderer(string rendererName, bool shadow)
    {
        GameObject arrowHead = new GameObject(rendererName);
        arrowHead.transform.SetParent(transform, false);
        LineRenderer renderer = arrowHead.AddComponent<LineRenderer>();
        ConfigureRenderer(
            renderer,
            shadow ? 0.085f : 0.055f,
            shadow ? 0.050f : 0.030f,
            shadow ? new Color(0.02f, 0.025f, 0.03f, 0.78f) : new Color(0.96f, 0.98f, 1f, 0.98f),
            shadow ? new Color(0.02f, 0.025f, 0.03f, 0.78f) : new Color(0.78f, 0.88f, 1f, 0.98f));
        return renderer;
    }

    private void ConfigureRenderer(LineRenderer renderer, float startWidth, float endWidth, Color startColor, Color endColor)
    {
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.positionCount = 2;
        renderer.startWidth = startWidth;
        renderer.endWidth = endWidth;
        renderer.useWorldSpace = true;
        renderer.startColor = startColor;
        renderer.endColor = endColor;
    }

    private void UpdateArrowGeometry(Vector3 raisedStart, Vector3 raisedEnd)
    {
        Vector3 flatDirection = raisedEnd - raisedStart;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude <= 0.001f)
        {
            SetLine(shadowRenderer, raisedStart, raisedEnd);
            SetLine(lineRenderer, raisedStart + Vector3.up * 0.01f, raisedEnd + Vector3.up * 0.01f);
            return;
        }

        Vector3 direction = flatDirection.normalized;
        Vector3 perpendicular = new Vector3(-direction.z, 0f, direction.x);
        float headLength = 0.40f;
        float headWidth = 0.24f;
        Vector3 shaftEnd = raisedEnd - direction * headLength * 0.55f;
        Vector3 headBase = raisedEnd - direction * headLength;
        Vector3 leftPoint = headBase + perpendicular * headWidth;
        Vector3 rightPoint = headBase - perpendicular * headWidth;

        Vector3 visualLift = Vector3.up * 0.012f;
        SetLine(shadowRenderer, raisedStart, shaftEnd);
        SetLine(lineRenderer, raisedStart + visualLift, shaftEnd + visualLift);
        SetLine(leftHeadShadowRenderer, raisedEnd, leftPoint);
        SetLine(rightHeadShadowRenderer, raisedEnd, rightPoint);
        SetLine(leftHeadRenderer, raisedEnd + visualLift, leftPoint + visualLift);
        SetLine(rightHeadRenderer, raisedEnd + visualLift, rightPoint + visualLift);
    }

    private static void SetLine(LineRenderer renderer, Vector3 start, Vector3 end)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.SetPosition(0, start);
        renderer.SetPosition(1, end);
    }

    private TextMesh CreateLabel(string name, float characterSize, Color color)
    {
        GameObject labelObject = new GameObject(name);
        labelObject.transform.SetParent(transform, false);
        TextMesh textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = characterSize;
        textMesh.fontSize = 96;
        textMesh.color = color;
        return textMesh;
    }

    private GameObject CreateSkullIcon(string name)
    {
        GameObject skull = GameObject.CreatePrimitive(PrimitiveType.Quad);
        skull.name = name;
        skull.transform.SetParent(transform, false);
        skull.transform.localScale = new Vector3(0.22f, 0.22f, 1f);
        Collider collider = skull.GetComponent<Collider>();
        if (collider != null)
        {
            RuntimeSafeDestroy.Destroy(collider);
        }

        MeshRenderer renderer = skull.GetComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        Texture2D skullTexture = SceneIconRegistry.Active != null
            ? SceneIconRegistry.Active.EstimatedDeathSkullIcon
            : Resources.Load<Texture2D>("Icons/EstimatedDeathSkull");
        if (skullTexture != null)
        {
            renderer.material.mainTexture = skullTexture;
        }

        skull.SetActive(false);
        return skull;
    }

    private void UpdateDamageOverlay(TextMesh damageLabel, GameObject skullObject, MeshRenderer skullRenderer, Vector3 position, int damage, bool lethal)
    {
        if (damageLabel == null || skullObject == null)
        {
            return;
        }

        if (!lethal && damage <= 0)
        {
            HideDamageOverlay(damageLabel, skullObject);
            return;
        }

        damageLabel.transform.position = position + Vector3.up * 0.08f;
        damageLabel.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        if (lethal)
        {
            damageLabel.text = string.Empty;
            skullObject.SetActive(true);
            skullObject.transform.position = position + Vector3.up * 0.06f;
            skullObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            if (skullRenderer != null)
            {
                skullRenderer.enabled = true;
            }
            return;
        }

        skullObject.SetActive(false);
        damageLabel.text = damage.ToString();
    }

    private void HideDamageOverlay(TextMesh damageLabel, GameObject skullObject)
    {
        if (damageLabel != null)
        {
            damageLabel.text = string.Empty;
        }

        if (skullObject != null)
        {
            skullObject.SetActive(false);
        }
    }
}

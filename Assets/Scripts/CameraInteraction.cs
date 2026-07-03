using UnityEngine;

public class CameraInteraction : MonoBehaviour
{
    [SerializeField] private bool enableKeyboardPan = true;
    [SerializeField] private float keyboardPanSpeed = 2.2f;
    [SerializeField] private float keyboardPanRangeX = 1.15f;
    [SerializeField] private float keyboardPanRangeZ = 0.78f;
    [SerializeField] private float keyboardPanSmooth = 9.0f;

    private Vector3 originalPos;
    private Vector3 targetPanOffset;
    private Vector3 panOffset;
    private Vector3 shakeOffset;

    public Vector3 ViewOffset => panOffset;

    void Start() => originalPos = transform.localPosition;

    private void Update()
    {
        UpdateKeyboardPan();
        ApplyCameraOffset();
    }

    public void ShakeCamera(float intensity, float duration = 0.2f)
    {
        StopAllCoroutines();
        StartCoroutine(ShakeRoutine(intensity, duration));
    }

    private void UpdateKeyboardPan()
    {
        if (!enableKeyboardPan)
        {
            targetPanOffset = Vector3.zero;
            panOffset = Vector3.Lerp(panOffset, targetPanOffset, 1f - Mathf.Exp(-keyboardPanSmooth * Time.deltaTime));
            return;
        }

        Vector3 input = Vector3.zero;
        if (Input.GetKey(KeyCode.A))
        {
            input.x -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            input.x += 1f;
        }

        if (Input.GetKey(KeyCode.W))
        {
            input.z += 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            input.z -= 1f;
        }

        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        targetPanOffset += input * keyboardPanSpeed * Time.deltaTime;
        targetPanOffset.x = Mathf.Clamp(targetPanOffset.x, -keyboardPanRangeX, keyboardPanRangeX);
        targetPanOffset.y = 0f;
        targetPanOffset.z = Mathf.Clamp(targetPanOffset.z, -keyboardPanRangeZ, keyboardPanRangeZ);
        panOffset = Vector3.Lerp(panOffset, targetPanOffset, 1f - Mathf.Exp(-keyboardPanSmooth * Time.deltaTime));
    }

    private void ApplyCameraOffset()
    {
        transform.localPosition = originalPos + panOffset + shakeOffset;
    }

    private System.Collections.IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            shakeOffset = Random.insideUnitSphere * intensity;
            shakeOffset.y = 0f;
            ApplyCameraOffset();
            yield return null;
        }
        shakeOffset = Vector3.zero;
        ApplyCameraOffset();
    }
}

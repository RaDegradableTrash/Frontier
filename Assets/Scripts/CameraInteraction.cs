using UnityEngine;

public class CameraInteraction : MonoBehaviour
{
    private Vector3 originalPos;

    void Start() => originalPos = transform.localPosition;

    public void ShakeCamera(float intensity, float duration = 0.2f)
    {
        StopAllCoroutines();
        StartCoroutine(ShakeRoutine(intensity, duration));
    }

    private System.Collections.IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localPosition = originalPos + Random.insideUnitSphere * intensity;
            yield return null;
        }
        transform.localPosition = originalPos;
    }
}
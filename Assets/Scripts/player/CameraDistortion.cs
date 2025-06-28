using UnityEngine;
using System.Collections;

public class CameraDistortion : MonoBehaviour
{
    public Transform cameraTransform;
    private Coroutine distortionCoroutine;

    private void Awake()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    public void StartDistortion(float duration, float intensity)
    {
        if (distortionCoroutine != null)
        {
            StopCoroutine(distortionCoroutine);
        }
        distortionCoroutine = StartCoroutine(Distort(duration, intensity));
    }

    private IEnumerator Distort(float duration, float intensity)
    {
        Quaternion originalRotation = cameraTransform.localRotation;
        float endTime = Time.time + duration;

        while (Time.time < endTime)
        {
            float x = (Mathf.PerlinNoise(Time.time * 10f, 0) - 0.5f) * 2f * intensity;
            float y = (Mathf.PerlinNoise(0, Time.time * 10f) - 0.5f) * 2f * intensity;

            cameraTransform.localRotation = originalRotation * Quaternion.Euler(x, y, 0);
            yield return null;
        }

        cameraTransform.localRotation = originalRotation;
    }
}
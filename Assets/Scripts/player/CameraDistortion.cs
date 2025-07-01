using UnityEngine;
using System.Collections;

public class CameraDistortion : MonoBehaviour
{
    public Transform cameraTransform;
    private Camera mainCamera;
    private Coroutine distortionCoroutine;

    private void Awake()
    {
        if (cameraTransform == null)
        {
            mainCamera = Camera.main;
            cameraTransform = mainCamera.transform;
        }
        else
        {
            mainCamera = cameraTransform.GetComponent<Camera>();
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
        float originalFOV = mainCamera.fieldOfView;
        float endTime = Time.time + duration;

        float swaySpeed = 0.5f;
        float fovPulseSpeed = 0.75f;
        float rollIntensity = 10f;
        float fovIntensity = 5f;

        while (Time.time < endTime)
        {
            // Original shaky effect
            float x = (Mathf.PerlinNoise(Time.time * 10f, 0) - 0.5f) * 2f * intensity;
            float y = (Mathf.PerlinNoise(0, Time.time * 10f) - 0.5f) * 2f * intensity;

            // Add a swaying, rolling motion on the Z-axis
            float zRoll = Mathf.Sin(Time.time * swaySpeed) * rollIntensity;

            // Add a pulsating FOV effect
            float fovPulse = Mathf.Sin(Time.time * fovPulseSpeed) * fovIntensity;
            mainCamera.fieldOfView = originalFOV + fovPulse;

            cameraTransform.localRotation = originalRotation * Quaternion.Euler(x, y, zRoll);
            yield return null;
        }

        // Reset camera to its original state
        cameraTransform.localRotation = originalRotation;
        mainCamera.fieldOfView = originalFOV;
    }
}
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraOptmization : MonoBehaviour
{
    private Camera _camera;

    [Tooltip("The maximum distance the camera can see. Lowering this can improve performance.")]
    [SerializeField] private float farClipPlaneDistance = 1000f;

    void Start()
    {
        _camera = GetComponent<Camera>();
        _camera.farClipPlane = farClipPlaneDistance;
    }
}
using UnityEngine;

public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            // Make the quad face the camera
            transform.LookAt(Camera.main.transform.position, Vector3.up);
            // Flip 180 degrees because Unity's quad front is -Z
            transform.Rotate(0, 180f, 0);
        }
    }
}

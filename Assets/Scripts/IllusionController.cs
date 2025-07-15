using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages the activation and deactivation of illusionary walls and blockers in the scene.
/// This component should be placed on the Player object.
/// </summary>
public class IllusionController : MonoBehaviour
{
    [Tooltip("The tag for objects that are normally solid but become passable during the illusion.")]
    public string illusionWallTag = "IllusionWall";

    [Tooltip("The tag for objects that are normally passable but become solid during the illusion.")]
    public string illusionBlockerTag = "IllusionBlocker";

    private List<GameObject> illusionWalls;
    private List<GameObject> illusionBlockers;

    private bool isIllusionActive = false;

    void Start()
    {
        // Find and cache all illusion objects at startup for better performance.
        illusionWalls = new List<GameObject>(GameObject.FindGameObjectsWithTag(illusionWallTag));
        illusionBlockers = new List<GameObject>(GameObject.FindGameObjectsWithTag(illusionBlockerTag));

        // Initially, ensure blockers are inactive.
        foreach (var blocker in illusionBlockers)
        {
            SetObjectState(blocker, false);
        }
    }

    /// <summary>
    /// Activates the illusion for a specified duration.
    /// </summary>
    /// <param name="duration">How long the illusion should last, in seconds.</param>
    public void TriggerIllusion(float duration)
    {
        if (!isIllusionActive)
        {
            StartCoroutine(IllusionSequence(duration));
        }
    }

    private IEnumerator IllusionSequence(float duration)
    {
        isIllusionActive = true;

        // --- Activate the illusion ---
        // Make solid walls appear to vanish.
        foreach (var wall in illusionWalls)
        {
            SetObjectState(wall, false);
        }

        // Make previously open paths appear as solid walls.
        foreach (var blocker in illusionBlockers)
        {
            SetObjectState(blocker, true);
        }

        // Wait for the specified duration.
        yield return new WaitForSeconds(duration);

        // --- Deactivate the illusion ---
        // Restore the solid walls.
        foreach (var wall in illusionWalls)
        {
            SetObjectState(wall, true);
        }

        // Remove the illusionary blockers.
        foreach (var blocker in illusionBlockers)
        {
            SetObjectState(blocker, false);
        }

        isIllusionActive = false;
    }

    /// <summary>
    /// Helper method to enable or disable an object's renderer and collider.
    /// </summary>
    private void SetObjectState(GameObject obj, bool active)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = active;
        }

        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = active;
        }
    }
}
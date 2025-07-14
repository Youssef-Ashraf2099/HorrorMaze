using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Image Effects/Glitch Effect")]
public class GlitchEffect : MonoBehaviour
{
    public Shader shader;
    [Range(0, 1)]
    public float intensity = 0.5f;
    [Range(0, 1)]
    public float flipIntensity = 0.5f;
    [Range(0, 1)]
    public float colorIntensity = 0.5f;

    private Material material;

    void OnEnable()
    {
        if (shader != null)
        {
            material = new Material(shader);
        }
    }

    void OnDisable()
    {
        if (material != null)
        {
            DestroyImmediate(material);
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (material == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        material.SetFloat("_Intensity", intensity);
        material.SetFloat("_FlipIntensity", flipIntensity);
        material.SetFloat("_ColorIntensity", colorIntensity);

        Graphics.Blit(source, destination, material);
    }
}
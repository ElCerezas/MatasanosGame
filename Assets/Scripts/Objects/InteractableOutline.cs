using System.Collections.Generic;
using UnityEngine;

public class InteractableOutline : MonoBehaviour
{
    [SerializeField] Renderer[] targetRenderers;

    [SerializeField] Color outlineColor = Color.white;
    [SerializeField] float outlineWidth = 0.02f;
    [SerializeField] bool isHighlighted;

    static Shader outlineShader;
    Material outlineMaterial;

    void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>();

        if (outlineShader  == null)
            outlineShader = Shader.Find("Shader Graphs/OutlineShader");

        outlineMaterial = new Material(outlineShader){ hideFlags = HideFlags.HideAndDontSave };
    }

    public void SetHighlight(bool active)
    {
        if (isHighlighted == active || outlineMaterial == null) return;
        
        isHighlighted = active;
        foreach (var r in targetRenderers)
        {
            if (r == null) continue;

            var mats = new List<Material>(r.sharedMaterials);

            if (active)
                mats.Add(outlineMaterial);
            else
                mats.Remove(outlineMaterial);

            r.materials = mats.ToArray();
        }
    }

    void OnDisable() => SetHighlight(false);

    void OnDestroy()
    {
        SetHighlight(false);
        if (outlineMaterial != null)
            Destroy(outlineMaterial);
    }
}
using System.Collections.Generic;
using UnityEngine;

public class InteractableOutline : MonoBehaviour
{
    [SerializeField] Renderer[] targetRenderers;
    [SerializeField] bool isHighlighted;

    Material outlineMaterial;

    void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>();

        Material loadedMat = Resources.Load<Material>("OutlineMaterial");
        if (loadedMat == null) return;
        outlineMaterial = new Material(loadedMat) { hideFlags = HideFlags.HideAndDontSave };
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
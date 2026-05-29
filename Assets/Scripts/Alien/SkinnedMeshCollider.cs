using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class SkinnedMeshCollider : MonoBehaviour
{
    private MeshCollider meshCollider;
    private SkinnedMeshRenderer skinnedMesh;
    private Mesh bakedMesh;

    [SerializeField] float updateInterval = 0.1f;
    float timer;

    void Awake()
    {
        meshCollider = GetComponent<MeshCollider>();
        skinnedMesh = GetComponent<SkinnedMeshRenderer>();
        bakedMesh = new Mesh();
        meshCollider.convex = false;
        BakeCollider();
    }

    void LateUpdate()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            BakeCollider();
        }
    }

    public void BakeCollider()
    {
        skinnedMesh.BakeMesh(bakedMesh);
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = bakedMesh;
    }
}
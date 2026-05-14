using System.Collections;
using Unity.Netcode;
using UnityEngine;
public class Impresora3D : PoweredDevice
{
    [Header("Config")]
    [SerializeField] PrintingObject[] printableObjects;
    [SerializeField] GameObject hologramRenderer;

    [Header("Visual")]
    NetworkVariable<int> selectedIndex = new NetworkVariable<int>(0);
    [Range(0f,1f)]NetworkVariable<float> printProgress = new NetworkVariable<float>(0f);

    Coroutine printCoroutine;

    [SerializeField] Renderer hologramVisualRenderer;
    [SerializeField] MaterialPropertyBlock mpb;

    void Awake()
    {
        hologramVisualRenderer = hologramRenderer.GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        LoadHolo(selectedIndex.Value);
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    void LoadHolo(int index)
    {
        hologramRenderer.GetComponent<MeshFilter>().mesh = printableObjects[index].printingObject.GetComponent<MeshFilter>().sharedMesh;
        hologramRenderer.GetComponent<Renderer>().enabled = hasPower.Value && hologramRenderer.GetComponent<SnapZone>().currentItem == null;
        SetupMeshBounds();
    }
    IEnumerator PrintCoroutine()
    {
        float duration = printableObjects[selectedIndex.Value].printingTime;
        float localProgress = printProgress.Value;
        float syncTimer = 0f;
        const float syncInterval = 0.1f;

        while (localProgress < 1f)
        {
            if (!hasPower.Value)
            {
                yield return null;
                continue;
            }

            float delta = Time.deltaTime / duration;
            localProgress = Mathf.Clamp01(localProgress + delta);
            syncTimer += Time.deltaTime;
            if (syncTimer >= syncInterval)
            {
                printProgress.Value = localProgress;
                syncTimer = 0f;
                UpdatePrintShader();
            }
            yield return null;
        }
        printProgress.Value = 1f;
        yield return null;
        FinishPrint();
    }
    void FinishPrint()
    {
        if (!IsServer) return;
        GameObject prefab = printableObjects[selectedIndex.Value].printingObject;
        GameObject instance = Instantiate(prefab, hologramRenderer.transform.position, hologramRenderer.transform.rotation);
        instance.GetComponent<NetworkObject>().Spawn();

        if (instance.TryGetComponent(out SnappableItem snappable))
        {
            snappable.SnapTo(hologramRenderer.GetComponent<SnapZone>());
            hologramRenderer.GetComponent<SnapZone>().currentItem = snappable;
        }

        printProgress.Value = 0f;
        UpdatePrintShader();
        LoadHolo(selectedIndex.Value);
    }
    public void OnNextPrint(int i)
    {
        if (printProgress.Value > 0f) return;
        selectedIndex.Value += i;
        LoadHolo(selectedIndex.Value);
    }
    public void RequestPrint()
    {
        if (!IsServer) return;

        if (printProgress.Value > 0f) return;
        if (!hasPower.Value) return;
        if (hologramRenderer.GetComponent<SnapZone>().currentItem != null) return;

        Debug.Log("PrintStart");
        printProgress.Value = 0.001f;
        UpdatePrintShader();
        printCoroutine = StartCoroutine(PrintCoroutine());
    }
    public override void Powered()
    {
        LoadHolo(selectedIndex.Value);
    }

    void UpdatePrintShader()
    {
        hologramVisualRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_PrintingPercent", printProgress.Value);
        hologramVisualRenderer.SetPropertyBlock(mpb);
    }
    void SetupMeshBounds()
    {
        Mesh mesh = hologramRenderer.GetComponent<MeshFilter>().sharedMesh;
        Bounds b = mesh.bounds;

        hologramVisualRenderer.GetPropertyBlock(mpb);

        hologramVisualRenderer.SetPropertyBlock(mpb);
    }

}
[System.Serializable]
public struct PrintingObject
{
    public string displayName;
    public float printingTime;
    public GameObject printingObject;
}
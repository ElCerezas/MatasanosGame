using System.Collections;
using Unity.Netcode;
using UnityEngine;
public class Impresora3D : PoweredDevice
{
    [Header("Config")]
    [SerializeField] PrintingObject[] printableObjects;
    [SerializeField] Transform printingAnchor;
    [SerializeField] SnapZone anchorSnapZone;

    [Header("Visual")]
    [SerializeField] GameObject hologramRenderer;
    NetworkVariable<int> selectedIndex = new NetworkVariable<int>(0);
    NetworkVariable<float> printProgress = new NetworkVariable<float>(0f);

    Coroutine printCoroutine;
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
            }
            Debug.Log($"Printing: {localProgress}/{duration}");
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
        GameObject instance = Instantiate(prefab, printingAnchor.position, printingAnchor.rotation);
        instance.GetComponent<NetworkObject>().Spawn();

        if (instance.TryGetComponent(out SnappableItem snappable))
            anchorSnapZone.currentItem = snappable;

        printProgress.Value = 0f;
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
        if (anchorSnapZone.currentItem != null) return;

        Debug.Log("PrintStart");
        printProgress.Value = 0.001f;
        printCoroutine = StartCoroutine(PrintCoroutine());
    }

    public override void Powered()
    {
    }
}
[System.Serializable]
public struct PrintingObject
{
    public string displayName;
    public float printingTime;
    public GameObject printingObject;
}
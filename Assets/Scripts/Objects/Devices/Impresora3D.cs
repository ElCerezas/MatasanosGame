using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Impresora3D : PoweredDevice
{
    [Header("Config")]
    [SerializeField] PrintingObject[] printableObjects;
    [SerializeField] GameObject hologramRenderer;

    [Header("Visual")]
    NetworkVariable<int> selectedIndex = new NetworkVariable<int>(0);
    [Range(0f, 1f)]
    NetworkVariable<float> printProgress = new NetworkVariable<float>(0f);
    [SerializeField] TextMeshProUGUI screenText;

    Coroutine printCoroutine;

    [SerializeField] Renderer hologramVisualRenderer;
    MeshFilter meshFilter;
    SnapZone snapZone;
    [SerializeField] MaterialPropertyBlock mpb;

    static readonly int ID_PrintingPercent = Shader.PropertyToID("_PrintingPercent");
    static readonly int ID_MainTexture = Shader.PropertyToID("_MainTexture");

    void Awake()
    {
        hologramVisualRenderer = hologramRenderer.GetComponent<Renderer>();
        meshFilter = hologramRenderer?.GetComponent<MeshFilter>();
        snapZone = hologramRenderer?.GetComponent<SnapZone>();
        mpb = new MaterialPropertyBlock();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        LoadHolo();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }
    public void LoadHolo()
    {
        GameObject sourcePrefab = printableObjects[selectedIndex.Value].printingObject;
        meshFilter.mesh = sourcePrefab.GetComponent<MeshFilter>().sharedMesh;
        hologramVisualRenderer.enabled = hasPower.Value && snapZone.currentItem == null;

        screenText.enabled = hasPower.Value;
        screenText.text = printableObjects[selectedIndex.Value].displayName;
        PushSourceTexture(sourcePrefab);
    }
    void PushSourceTexture(GameObject sourcePrefab)
    {
        MeshRenderer sourceRenderer = sourcePrefab.GetComponent<MeshRenderer>();
        if (sourceRenderer == null) return;

        Texture sourceTexture = sourceRenderer.sharedMaterial?.mainTexture;

        hologramVisualRenderer.GetPropertyBlock(mpb);

        if (sourceTexture != null)
            mpb.SetTexture(ID_MainTexture, sourceTexture);

        hologramVisualRenderer.SetPropertyBlock(mpb);
    }
    IEnumerator PrintCoroutine()
    {
        float duration = printableObjects[selectedIndex.Value].printingTime;
        float localProgress = printProgress.Value;
        float syncTimer = 0f;
        const float syncInterval = 0.1f;
        const int barLength = 10;

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

            UpdatePrintShader(localProgress);

            if (syncTimer >= syncInterval)
            {
                printProgress.Value = localProgress;
                int filledLength = Mathf.RoundToInt(localProgress * barLength);
                string filledPart = new string('#', filledLength);
                string emptyPart = new string('_', barLength - filledLength);
                screenText.text = $"[{filledPart}{emptyPart}]\n{Mathf.FloorToInt(localProgress * 100)}%";
                syncTimer = 0f;
            }

            yield return null;
        }

        printProgress.Value = 1f;
        UpdatePrintShader(1f);
        yield return null;
        FinishPrint();
    }

    void FinishPrint()
    {
        if (!IsServer) return;

        GameObject prefab = printableObjects[selectedIndex.Value].printingObject;
        GameObject instance = Instantiate(prefab,
            hologramRenderer.transform.position,
            hologramRenderer.transform.rotation);
        instance.GetComponent<NetworkObject>().Spawn();

        if (instance.TryGetComponent(out SnappableItem snappable))
        {
            snappable.SnapTo(hologramRenderer.GetComponent<SnapZone>());
            hologramRenderer.GetComponent<SnapZone>().currentItem = snappable;
        }

        printProgress.Value = 0f;
        UpdatePrintShader(0f);
        LoadHolo();
    }
    public void OnNextPrint(int i)
    {
        if (printProgress.Value > 0f) return;
        selectedIndex.Value += i;
        Debug.Log(selectedIndex.Value);
        selectedIndex.Value = selectedIndex.Value % (printableObjects.Length-1);
        LoadHolo();
    }

    public void RequestPrint()
    {
        if (!IsServer) return;
        if (printProgress.Value > 0f) return;
        if (!hasPower.Value) return;
        if (hologramRenderer.GetComponent<SnapZone>().currentItem != null) return;

        Debug.Log("PrintStart");
        PushSourceTexture(printableObjects[selectedIndex.Value].printingObject);

        printProgress.Value = 0.001f;
        UpdatePrintShader(0.001f);
        printCoroutine = StartCoroutine(PrintCoroutine());
    }

    public override void Powered()
    {
        LoadHolo();
    }
    void UpdatePrintShader(float progress)
    {
        hologramVisualRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(ID_PrintingPercent, progress);
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
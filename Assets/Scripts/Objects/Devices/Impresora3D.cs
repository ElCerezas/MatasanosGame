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
    NetworkVariable<bool> snapZoneOccupied = new NetworkVariable<bool>(false);
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

        snapZone.OnObjectSnapped.AddListener(OnItemSnapped);
        snapZone.OnObjectUnsnapped.AddListener(OnItemUnsnapped);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        selectedIndex.OnValueChanged += OnSelectedIndexChanged;
        printProgress.OnValueChanged += OnPrintProgressChanged;
        snapZoneOccupied.OnValueChanged += OnSnapZoneOccupiedChanged;
        hasPower.OnValueChanged += OnHasPowerChanged;

        LoadHolo();
    }

    public override void OnNetworkDespawn()
    {
        selectedIndex.OnValueChanged -= OnSelectedIndexChanged;
        printProgress.OnValueChanged -= OnPrintProgressChanged;
        snapZoneOccupied.OnValueChanged -= OnSnapZoneOccupiedChanged;
        hasPower.OnValueChanged -= OnHasPowerChanged;

        snapZone.OnObjectSnapped.RemoveListener(OnItemSnapped);
        snapZone.OnObjectUnsnapped.RemoveListener(OnItemUnsnapped);

        base.OnNetworkDespawn();
    }

    void OnSelectedIndexChanged(int old, int next) => LoadHolo();
    void OnPrintProgressChanged(float old, float next) { UpdatePrintShader(next); UpdateScreenText(next); }
    void OnSnapZoneOccupiedChanged(bool old, bool next) => LoadHolo();
    void OnHasPowerChanged(bool old, bool next) => LoadHolo();

    void OnItemSnapped()
    {
        if (IsServer) snapZoneOccupied.Value = true;
    }

    void OnItemUnsnapped()
    {
        if (IsServer) snapZoneOccupied.Value = false;
    }

    public void LoadHolo()
    {
        GameObject sourcePrefab = printableObjects[selectedIndex.Value].printingObject;
        meshFilter.mesh = sourcePrefab.GetComponent<MeshFilter>().sharedMesh;
        hologramVisualRenderer.enabled = hasPower.Value && !snapZoneOccupied.Value;

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
        snapZoneOccupied.Value = true;
        UpdatePrintShader(0f);
        LoadHolo();
    }

    public void OnNextPrint(int i)
    {
        OnNextPrintServerRpc(i);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void OnNextPrintServerRpc(int i)
    {
        if (printProgress.Value > 0f) return;
        selectedIndex.Value = (selectedIndex.Value + i + printableObjects.Length) % printableObjects.Length;
    }

    public void RequestPrint()
    {
        RequestPrintServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void RequestPrintServerRpc()
    {
        if (printProgress.Value > 0f) return;
        if (!hasPower.Value) return;
        if (snapZoneOccupied.Value) return;

        printProgress.Value = 0.001f;
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

    void UpdateScreenText(float progress)
    {
        if (!screenText.enabled) return;
        const int barLength = 10;
        int filled = Mathf.RoundToInt(progress * barLength);
        screenText.text = $"[{new string('#', filled)}{new string('_', barLength - filled)}]\n{Mathf.FloorToInt(progress * 100)}%";
    }
}

[System.Serializable]
public struct PrintingObject
{
    public string displayName;
    public float printingTime;
    public GameObject printingObject;
}
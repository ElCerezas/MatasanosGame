using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerVisual : NetworkBehaviour
{
    [Header("Visuals")]
    [SerializeField] Renderer playerRenderer;
    [SerializeField] Material playerMaterial;

    [Header("Face Decals")]
    [SerializeField] DecalProjector eyeDecalRenderer;
    [SerializeField] DecalProjector mouthDecalRenderer;
    [SerializeField] int sheetColumns = 1;
    [SerializeField] int sheetRows = 1;
    [NonSerialized] public NetworkVariable<int> eyeFaceIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [NonSerialized] public NetworkVariable<int> mouthFaceIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    int totalFaces = 1;

    [Header("Hat Visuals")]
    [SerializeField] MeshFilter hatMeshFilter;
    [SerializeField] MeshRenderer hatRenderer;

    [Header("Dirt Settings")]
    [SerializeField] float parasiteGrowthRate = 0.05f;
    [SerializeField] float maxSplashes = 5f;
    [NonSerialized]
    public NetworkVariable<int> bloodSplashes = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [NonSerialized]
    public NetworkVariable<int> mocoSplashes = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [NonSerialized]
    public NetworkVariable<float> networkParasiteIntensity = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] float currentParasiteIntensity = 0f;

    private void Awake()
    {
        playerMaterial = playerRenderer.material;
        totalFaces = sheetRows * sheetColumns;
    }

    public override void OnNetworkSpawn()
    {
        bloodSplashes.OnValueChanged += (oldVal, newVal) => UpdateMaterialSplashes();
        mocoSplashes.OnValueChanged += (oldVal, newVal) => UpdateMaterialSplashes();

        eyeFaceIndex.OnValueChanged += (oldVal, newVal) => UpdateEyeDecal();
        mouthFaceIndex.OnValueChanged += (oldVal, newVal) => UpdateMouthDecal();

        Vector2 s = new Vector2(1f / sheetColumns, 1f / sheetRows);
        eyeDecalRenderer.uvScale = s;
        mouthDecalRenderer.uvScale = s;

        UpdateMaterialSplashes();
        UpdateEyeDecal();
        UpdateMouthDecal();
    }

    private void Update()
    {
        if (!IsOwner)
        {
            if (Mathf.Abs(currentParasiteIntensity - networkParasiteIntensity.Value) > 0.001f)
            {
                currentParasiteIntensity = networkParasiteIntensity.Value;
                UpdateParasiteMaterial();
            }
        }
    }

    #region Dirty
    public void SetParasiteIntensityLocal(float value)
    {
        currentParasiteIntensity = value;
        UpdateParasiteMaterial();
    }
    void UpdateMaterialSplashes()
    {
        if (playerMaterial == null) return;
        float bloodAmount = Mathf.Clamp01(bloodSplashes.Value / maxSplashes);
        float mocoAmount = Mathf.Clamp01(mocoSplashes.Value / maxSplashes);
        playerMaterial.SetFloat("_BloodAmount", bloodAmount);
        playerMaterial.SetFloat("_MocoAmount", mocoAmount);
    }
    void UpdateParasiteMaterial()
    {
        if (playerMaterial == null) return;
        playerMaterial.SetFloat("_ParasiteAmount", currentParasiteIntensity);
    }
    #endregion

    #region Customitzation
    public void ChangeColor(Color colr)
    {
        playerMaterial.SetColor("_PlayerColor", colr);
    }
    public void ChangeHat(Mesh hatMesh, Material hatMaterial)
    {
        if (hatMeshFilter != null && hatRenderer != null)
        {
            hatMeshFilter.mesh = hatMesh;
            hatRenderer.material = hatMaterial;
        }
        else
        {
            Debug.LogWarning("Falta asignar el Hat Mesh Filter o el Hat Renderer en el Inspector del PlayerVisual.");
        }
    }

    Vector2 GetSpriteSheetCoords(int index)
    {
        index = ((index % totalFaces) + totalFaces) % totalFaces;
        float tileW = 1f / sheetColumns;
        float tileH = 1f / sheetRows;

        float offsetX = (index % sheetColumns) * tileW;
        float offsetY = 1f - tileH - ((index / sheetColumns) * tileH);

        return new Vector2(offsetX, offsetY);
    }
    void UpdateEyeDecal()
    {
        if (eyeDecalRenderer == null) return;
        eyeDecalRenderer.uvBias = GetSpriteSheetCoords(eyeFaceIndex.Value);
    }
    void UpdateMouthDecal()
    {
        if (mouthDecalRenderer == null) return;
        mouthDecalRenderer.uvBias = GetSpriteSheetCoords(mouthFaceIndex.Value);
    }

    public void SetEyeFaceIndex(int index)
    {
        eyeFaceIndex.Value = ((index % totalFaces) + totalFaces) % totalFaces;
    }
    public void SetMouthFaceIndex(int index)
    {
        mouthFaceIndex.Value = ((index % totalFaces) + totalFaces) % totalFaces;
    }
    #endregion
}
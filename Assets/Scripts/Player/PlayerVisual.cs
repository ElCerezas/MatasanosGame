using System;
using TMPro;
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
    int totalFaces = 1;

    [Header("Hat Visuals")]
    [SerializeField] MeshFilter hatMeshFilter;
    [SerializeField] MeshRenderer hatRenderer;

    [Header("PlarNumber")]
    [SerializeField] TMP_Text ridText;

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


    [NonSerialized] public NetworkVariable<int> eyeFaceIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [NonSerialized] public NetworkVariable<int> mouthFaceIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [NonSerialized] public NetworkVariable<int> colorIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [NonSerialized] public NetworkVariable<int> hatIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [NonSerialized] public NetworkVariable<int> playerRID = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
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

        colorIndex.OnValueChanged += (oldVal, newVal) => ApplyColorFromIndex(newVal);
        hatIndex.OnValueChanged += (oldVal, newVal) => ApplyHatFromIndex(newVal);

        playerRID.OnValueChanged += (oldVal, newVal) => UpdateRIDText();

        Vector2 s = new Vector2(1f / sheetColumns, 1f / (sheetRows * 2f));
        eyeDecalRenderer.uvScale = s;
        mouthDecalRenderer.uvScale = s;

        UpdateMaterialSplashes();
        UpdateEyeDecal();
        UpdateMouthDecal();

        // Igual que con ojos/boca: aplicamos el estado ya sincronizado de
        // color y sombrero al spawnear. Esto es lo que faltaba para que
        // un jugador que se une tarde vea correctamente a los demás.
        ApplyColorFromIndex(colorIndex.Value);
        ApplyHatFromIndex(hatIndex.Value);

        if (IsServer)
        {
            if (playerRID.Value == 0)
            {
                playerRID.Value = UnityEngine.Random.Range(100, 1000);
            }
        }
        UpdateRIDText();
    }
    void Update()
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
        if (playerMaterial == null)
            playerMaterial = playerRenderer.material;

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

    // Traduce colorIndex -> Color usando las listas configuradas en
    // CharacterCustomitationMachine y lo aplica al material del jugador.
    void ApplyColorFromIndex(int index)
    {
        var machine = CharacterCustomitationMachine.Instance;
        if (machine == null || machine.colores == null) return;
        if (index < 0 || index >= machine.colores.Count) return;

        ChangeColor(machine.colores[index]);
    }

    // Traduce hatIndex -> Hat (mesh + material) y lo aplica al sombrero.
    void ApplyHatFromIndex(int index)
    {
        var machine = CharacterCustomitationMachine.Instance;
        if (machine == null || machine.hats == null) return;
        if (index < 0 || index >= machine.hats.Count) return;

        var hat = machine.hats[index];
        ChangeHat(hat.mesh, hat.mat);
    }

    Vector2 GetSpriteSheetCoords(int index, bool isMouth)
    {
        float tileW = 1f / sheetColumns;
        float tileH = 1f / (sheetRows * 2f);

        int col = index % sheetColumns;
        int row = index / sheetColumns;

        float offsetX = col * tileW;
        float offsetY = 1f - tileH - (row * tileH * 2f);

        if (isMouth)
            offsetY -= tileH;

        return new Vector2(offsetX, offsetY);
    }
    void UpdateEyeDecal()
    {
        if (eyeDecalRenderer == null) return;
        eyeDecalRenderer.uvBias = GetSpriteSheetCoords(eyeFaceIndex.Value, false);
    }
    void UpdateMouthDecal()
    {
        if (mouthDecalRenderer == null) return;
        mouthDecalRenderer.uvBias = GetSpriteSheetCoords(mouthFaceIndex.Value, true);
    }

    public void SetEyeFaceIndex(int index)
    {
        eyeFaceIndex.Value = ((index % totalFaces) + totalFaces) % totalFaces;
    }
    public void SetMouthFaceIndex(int index)
    {
        mouthFaceIndex.Value = ((index % totalFaces) + totalFaces) % totalFaces;
    }

    void UpdateRIDText()
    {
        if (ridText != null && playerRID.Value != 0)
        {
            ridText.text = playerRID.Value.ToString();
        }
    }
    #endregion
}
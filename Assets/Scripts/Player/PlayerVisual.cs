using Unity.Netcode;
using UnityEngine;

public class PlayerVisual : NetworkBehaviour
{
    [Header("Visuals")]
    [SerializeField] Renderer playerRenderer;
    [SerializeField] Material playerMaterial;

    [Header("Dirt Settings")]
    [SerializeField] float parasiteGrowthRate = 0.05f;
    [SerializeField] float maxSplashes = 5f;
    public NetworkVariable<int> bloodSplashes = new NetworkVariable<int>(0);
    public NetworkVariable<int> mocoSplashes = new NetworkVariable<int>(0);
    public NetworkVariable<float> networkParasiteIntensity = new NetworkVariable<float>(0f);
    [SerializeField]float currentParasiteIntensity = 0f;

    private void Awake()
    {
        playerMaterial = playerRenderer.material;
    }
    public override void OnNetworkSpawn()
    {
        bloodSplashes.OnValueChanged += (oldVal, newVal) => UpdateMaterialSplashes();
        mocoSplashes.OnValueChanged += (oldVal, newVal) => UpdateMaterialSplashes();

        UpdateMaterialSplashes();
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
}
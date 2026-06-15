using TMPro;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class MixerFlask : NetworkBehaviour
{
    private Material m;
    private NetworkVariable<float> fillAmount = new NetworkVariable<float>(0f);
    public NetworkVariable<LiquidType> liquid = new NetworkVariable<LiquidType>(LiquidType.Empty);
    public NetworkVariable<Color> color = new NetworkVariable<Color>(Color.white);
    public NetworkVariable<ColliderDetectorType> detectorType = new NetworkVariable<ColliderDetectorType>(ColliderDetectorType.Null);
    public NetworkVariable<bool> detectorEnabled = new NetworkVariable<bool>(false);
    
    [Header("Referencias de Componentes")]
    [SerializeField] private ColliderDetector colliderDetector;
    
    private void Awake()
    {
        if (colliderDetector == null)
            colliderDetector = GetComponent<ColliderDetector>();
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        m = GetComponent<Renderer>().material;
        
        fillAmount.OnValueChanged += (_, val) =>
        {
            UpdateLiquid(val);
            UpdateDetectorState(val);
        };
        color.OnValueChanged += (_, _) => UpdateLiquid(fillAmount.Value);
        detectorType.OnValueChanged += (_, type) => SyncDetectorType(type);
        detectorEnabled.OnValueChanged += (_, enabled) => SyncDetectorEnabled(enabled);
        
        UpdateLiquid(fillAmount.Value);
        UpdateDetectorState(fillAmount.Value);
    }
    
    public void Fill(LiquidType t, Color c)
    {
        if (!IsServer) return;
        
        liquid.Value = t;
        color.Value = c;
        fillAmount.Value = 1f;
        
        if (t == LiquidType.Betadine)
        {
            detectorType.Value = ColliderDetectorType.Betadine;
        }
        detectorEnabled.Value = true;
    }
    
    public void Empty(float amount)
    {
        if (IsServer)
        {
            ExecuteEmptyOnServer(amount);
        }
        else
        {
            EmptyServerRpc(amount);
        }
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void EmptyServerRpc(float amount)
    {
        ExecuteEmptyOnServer(amount);
    }
    
    private void ExecuteEmptyOnServer(float amount)
    {
        fillAmount.Value = Mathf.Clamp(fillAmount.Value - amount, 0f, 1f);
        detectorType.Value = ColliderDetectorType.Null;
        detectorEnabled.Value = false;
        
        if (fillAmount.Value <= 0f)
            liquid.Value = LiquidType.Empty;
    }
    
    private void UpdateLiquid(float newVal)
    {
        if (m == null) return;
        m.SetColor("_Color", color.Value);
        m.SetFloat("_FillAmount", Mathf.Min(newVal, 0.95f));
    }
    
    public void StartFilling(LiquidType t, Color c, float totalDuration, float initialFill)
    {
        if (IsServer)
        {
            StopAllCoroutines();
            StartCoroutine(FillCoroutine(t, c, totalDuration, initialFill));
        }
    }
    
    private IEnumerator FillCoroutine(LiquidType t, Color c, float totalDuration, float initialFill)
    {
        liquid.Value = t;
        color.Value = c;
        fillAmount.Value = initialFill;
        
        float elapsed = 0f;
        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            fillAmount.Value = Mathf.Lerp(initialFill, 1f, elapsed / totalDuration);
            yield return null;
        }
        
        fillAmount.Value = 1f;
        
        if (t == LiquidType.Betadine)
        {
            detectorType.Value = ColliderDetectorType.Betadine;
        }
        detectorEnabled.Value = true;
    }
    
    private void UpdateDetectorState(float currentFill)
    {
        if (IsServer)
        {
            detectorEnabled.Value = (currentFill >= 0.99f);
        }
    }
    
    private void SyncDetectorType(ColliderDetectorType newType)
    {
        if (colliderDetector != null)
        {
            colliderDetector.detectorType = newType;
        }
    }
    
    private void SyncDetectorEnabled(bool enabled)
    {
        if (colliderDetector != null)
        {
            colliderDetector.enabled = enabled;
        }
    }
}
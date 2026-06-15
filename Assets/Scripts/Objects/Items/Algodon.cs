using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

public class Algodon : NetworkBehaviour
{
    private NetworkVariable<bool> isWithBetadine = new NetworkVariable<bool>(false);
    
    [Header("Referencias")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    
    [Header("Assets Normal")]
    [SerializeField] private Material matNormal;
    [Header("Assets Betadine")]
    [SerializeField] private Material matBetadine;
    
    public override void OnNetworkSpawn()
    {
        isWithBetadine.OnValueChanged += OnVisualStateChanged;
        ApplyVisuals(isWithBetadine.Value);
    }
    
    private void OnVisualStateChanged(bool oldVal, bool newVal)
    {
        ApplyVisuals(newVal);
    }
    
    private void ApplyVisuals(bool hasBetadine)
    {
        if (meshRenderer != null && meshFilter != null)
        {
            meshRenderer.material = hasBetadine ? matBetadine : matNormal;
            GetComponent<ColliderInteractItem>().itemType = !hasBetadine ? ColliderItemType.Algodon : ColliderItemType.AlgodonEstirilizado;
        }
    }
    
    public void SetBetadine(bool state)
    {
        SetBetadineServerRpc(state);
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetBetadineServerRpc(bool state)
    {
        if (IsServer)
            isWithBetadine.Value = state;
    }
}
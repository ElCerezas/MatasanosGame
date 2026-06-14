using Unity.Netcode;
using UnityEngine;

public class InyeccionItem : NetworkBehaviour
{
    [SerializeField] private LiquidType inyeccionType;
    [SerializeField] private Renderer liquidRenderer;
    [SerializeField] private InyeccionCollider inyeccionCollider;
    [SerializeField] private Animation animator;
    [SerializeField] private AnimationClip Vacio;
    [SerializeField] private AnimationClip Inyectando;
    [SerializeField] private AnimationClip Lleno;
    [SerializeField] private AnimationClip Rellenando;

    [Header("Audio (FMOD)")]
    [SerializeField] private FMODUnity.EventReference sonidoVaciado;
    [SerializeField] private FMODUnity.EventReference sonidoLlenado;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        animator.Play(Vacio.name);
    }

    public void Inject(GameObject victim)
    {
        if (inyeccionType == LiquidType.Empty) return;

        var playerState = victim.GetComponent<PlayerStateManager>();
        if (playerState != null)
        {
            InjectPlayerServerRpc(victim.GetComponent<NetworkObject>().NetworkObjectId, (int)inyeccionType);
            return;
        }

        var alienState = victim.GetComponentInParent<AlienStateManager>();
        if (alienState != null)
        {
            var netObj = alienState.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                InjectAlienServerRpc(netObj.NetworkObjectId, (int)inyeccionType);
            }
            return;
        }
    }

    public void Fill(LiquidType t, Color color)
    {
        if (inyeccionType != LiquidType.Empty || t == LiquidType.Empty) return;
        
        FillServerRpc((int)t, color);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void InjectPlayerServerRpc(ulong playerNetObjId, int liquidType)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetObjId, out var obj))
        {
            if (obj.TryGetComponent<PlayerStateManager>(out var playerState))
            {
                playerState.ReceiveInjectionServerRpc(liquidType);
                inyeccionType = LiquidType.Empty; 
                PlayInjectClientRpc();
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void InjectAlienServerRpc(ulong alienNetObjId, int liquidType)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(alienNetObjId, out var obj))
        {
            if (obj.TryGetComponent<AlienStateManager>(out var alien))
            {
                alien.ApplyInyeccion((LiquidType)liquidType);
                inyeccionType = LiquidType.Empty;
                PlayInjectClientRpc();
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void FillServerRpc(int liquidType, Color color)
    {
        inyeccionType = (LiquidType)liquidType;
        PlayFillClientRpc(liquidType, color);
    }

    [Rpc(SendTo.Everyone)]
    void PlayInjectClientRpc()
    {
        inyeccionType = LiquidType.Empty; 
        
        animator.Play(Inyectando.name);
        animator.PlayQueued(Vacio.name);

        if (!sonidoVaciado.IsNull)
        {
            FMODUnity.RuntimeManager.PlayOneShotAttached(sonidoVaciado, gameObject);
        }
    }

    [Rpc(SendTo.Everyone)]
    void PlayFillClientRpc(int liquidType, Color color)
    {
        inyeccionType = (LiquidType)liquidType;

        Material m = liquidRenderer.sharedMaterial;
        m.SetColor("_Color", color);
        animator.Play(Rellenando.name);
        animator.PlayQueued(Lleno.name);

        if (!sonidoLlenado.IsNull)
        {
            FMODUnity.RuntimeManager.PlayOneShotAttached(sonidoLlenado, gameObject);
        }
    }
}
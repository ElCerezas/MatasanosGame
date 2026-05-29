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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        animator.Play(Vacio.name);
    }
    public void Inject(GameObject victim)
    {
        if (inyeccionType == LiquidType.Empty) return;

        // Intenta player primero
        var playerState = victim.GetComponent<PlayerStateManager>();
        if (playerState != null)
        {
            playerState.ReceiveInjectionServerRpc((int)inyeccionType);
            PlayInjectAnimation();
            return;
        }

        // Intenta alien
        var alienState = victim.GetComponentInParent<AlienStateManager>();
        if (alienState != null)
        {
            var netObj = alienState.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                InjectAlienServerRpc(netObj.NetworkObjectId, (int)inyeccionType);
                PlayInjectAnimation();
            }
            return;
        }
    }

    void PlayInjectAnimation()
    {
        inyeccionType = LiquidType.Empty;
        animator.Play(Inyectando.name);
        animator.PlayQueued(Vacio.name);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void InjectAlienServerRpc(ulong alienNetObjId, int liquidType)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(alienNetObjId, out var obj))
        {
            if (obj.TryGetComponent<AlienStateManager>(out var alien))
                alien.ApplyInyeccion((LiquidType)liquidType);
        }
    }
    public void Fill(LiquidType t, Color color)
    {
        if (inyeccionType != LiquidType.Empty || t == LiquidType.Empty) return;
        inyeccionType = t;
        Material m = liquidRenderer.sharedMaterial;
        m.SetColor("_Color", color);
        animator.Play(Rellenando.name);
        animator.PlayQueued(Lleno.name);
    }
}
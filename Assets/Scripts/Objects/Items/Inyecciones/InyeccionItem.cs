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
        var netObj = victim.GetComponentInParent<NetworkObject>();
        if (netObj != null)
        {
            LiquidType tipoAInyectar = inyeccionType;
            EventBus.Publish<OnInject>(new OnInject
            {
                VictimID = netObj.NetworkObjectId,
                Type = tipoAInyectar
            });
            inyeccionType = LiquidType.Empty;
            animator.Play(Inyectando.name);
            animator.PlayQueued(Vacio.name);
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
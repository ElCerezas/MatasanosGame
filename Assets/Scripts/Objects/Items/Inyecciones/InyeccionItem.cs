using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;

public class InyeccionItem : NetworkBehaviour
{
    [SerializeField] private LiquidType inyeccionType;
    [SerializeField] private InyeccionCollider inyeccionCollider;
    [SerializeField] private Animation animator;
    [SerializeField] private AnimationClip Inyectado;
    [SerializeField] private AnimationClip Inyectando;
    [SerializeField] private AnimationClip NoInyectado;
    private bool animPlayed = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        animator.Play(NoInyectado.name);
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
            animPlayed = true;
        }
    }

    public void Update()
    {
        if (!animator.IsPlaying(Inyectando.name) && animPlayed)
        {
            animator.Play(Inyectado.name);
        } 
    }
}
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class WoundVendaBehaviour : TaraBase
{
    public enum WoundState
    {
        Normal,
        Disinfected,
        Healed
    }

    private NetworkVariable<bool> isDesinfected = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isHealed = new NetworkVariable<bool>(false);
    private NetworkVariable<WoundState> woundState = new NetworkVariable<WoundState>(WoundState.Normal);

    [Header("Audio")]
    [SerializeField] private FMODUnity.EventReference sonidoDesinfectar;
    [SerializeField] private FMODUnity.EventReference sonidoCurar;

    private ColliderDetector colliderInteracttable;
    public DecalProjector woundProjector;
    public Material tiritaMaterial;
    public Material betadineMaterial;
    [SerializeField] ParticleSystem healingEffect;
    [SerializeField] float particleTime = 3f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        woundState.OnValueChanged += OnWoundStateChanged;
        
        if (!IsServer) return;
        EventBus.Publish(new TaraCreated { TaraID = NetworkObjectId, Type = type });
    }

    public override void OnNetworkDespawn()
    {
        woundState.OnValueChanged -= OnWoundStateChanged;
        base.OnNetworkDespawn();
    }

    private void Awake()
    {
        colliderInteracttable = gameObject.GetComponent<ColliderDetector>();
    }

    private void OnWoundStateChanged(WoundState oldState, WoundState newState)
    {
        switch (newState)
        {
            case WoundState.Normal:
                woundProjector.material = null;
                break;
            case WoundState.Disinfected:
                woundProjector.material = betadineMaterial;
                colliderInteracttable.detectorType = ColliderDetectorType.HeridaDesinfectada;
                break;
            case WoundState.Healed:
                woundProjector.material = tiritaMaterial;
                colliderInteracttable.detectorType = ColliderDetectorType.HeridaVendada;
                break;
        }
    }

    public void DesinfectedByCotton()
    {
        if (!isDesinfected.Value)
        {
            DesinfectServerRpc();
        }
    }

    public void HealedByBandage()
    {
        if (isDesinfected.Value && !isHealed.Value)
        {
            HealServerRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DesinfectServerRpc()
    {
        if (isDesinfected.Value) return;

        isDesinfected.Value = true;
        woundState.Value = WoundState.Disinfected;
        PlayParticlesClientRpc();
        Debug.Log("Desinfectada herida");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void HealServerRpc()
    {
        if (isHealed.Value) return;

        isHealed.Value = true;
        woundState.Value = WoundState.Healed;
        PlayParticlesClientRpc();
        MarkAsHealed();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayParticlesClientRpc()
    {
        StartCoroutine(ParticlesCorroutine());

        if (woundState.Value == WoundState.Disinfected && !sonidoDesinfectar.IsNull)
        {
            FMOD.Studio.EventInstance instDesinfectar = FMODUnity.RuntimeManager.CreateInstance(sonidoDesinfectar);
            instDesinfectar.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
            instDesinfectar.start();
            instDesinfectar.release();
        }
        else if (woundState.Value == WoundState.Healed && !sonidoCurar.IsNull)
        {
            FMOD.Studio.EventInstance instCurar = FMODUnity.RuntimeManager.CreateInstance(sonidoCurar);
            instCurar.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
            instCurar.start();
            instCurar.release();
        }
    }

    IEnumerator ParticlesCorroutine()
    {
        healingEffect.Play();
        yield return new WaitForSeconds(particleTime);
        healingEffect.Stop();
    }
}
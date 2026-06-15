using Unity.Netcode;
using UnityEngine;

public class WoundFocoBehaviour : TaraBase
{
    public float healingTimeRequired = 3f;
    private float healTimer = 0f;
    NetworkVariable<bool> isBeingHealed = new NetworkVariable<bool>(false);
    [SerializeField] ParticleSystem healingEffect;

    [Header("Audio")]
    [SerializeField] private FMODUnity.EventReference sonidoCuracionCompletada;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isBeingHealed.OnValueChanged += (oldVal, newVal) => {
            if (newVal)
                healingEffect.Play();
            else
                healingEffect.Stop();
        };
        if (!IsServer) return;
        EventBus.Publish(new TaraCreated { TaraID = NetworkObjectId, Type = type });
    }

    void Update()
    {
        if (!IsServer) return;

        if (isBeingHealed.Value)
        {
            healTimer += Time.deltaTime;
            if (healTimer >= healingTimeRequired)
            {
                MarkAsHealed();
                NotifyHealEndedClientRpc();
                GetComponent<NetworkObject>().Despawn();
                Destroy(gameObject);
            }
        }

        Debug.DrawLine(transform.position,
                       transform.position + Vector3.up * 2f,
                       isBeingHealed.Value ? Color.green : Color.red);
    }

    public void StartHealing()
    {
        if (!IsServer) return;
        isBeingHealed.Value = true;
        healTimer = 0f;
        NotifyHealStartedClientRpc();
    }

    public void StopHealing()
    {
        if (!IsServer) return;
        isBeingHealed.Value = false;
        healTimer = 0f;
    }

    [ClientRpc]
    public void NotifyHealStartedClientRpc()
    {
        EventBus.Publish(new OnWoundFocoHealStarted { TaraID = NetworkObjectId });
    }

    [ClientRpc]
    public void NotifyHealEndedClientRpc()
    {
        EventBus.Publish(new OnWoundFocoHealEnded { TaraID = NetworkObjectId });

        if (!sonidoCuracionCompletada.IsNull)
        {
            FMOD.Studio.EventInstance instanciaCuracion = FMODUnity.RuntimeManager.CreateInstance(sonidoCuracionCompletada);
            instanciaCuracion.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
            instanciaCuracion.start();
            instanciaCuracion.release(); 
        }
    }
}
using Unity.Netcode;
using UnityEngine;

public class WoundFocoBehaviour : TaraBase
{
    public float healingTimeRequired = 3f;
    private float healTimer = 0f;
    private bool isBeingHealed = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        EventBus.Publish(new TaraCreated { TaraID = NetworkObjectId, Type = type });
    }

    void Update()
    {
        if (!IsServer) return;

        if (isBeingHealed)
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
                       isBeingHealed ? Color.green : Color.red);
    }

    public void StartHealing()
    {
        if (!IsServer) return;
        isBeingHealed = true;
        healTimer = 0f;
        NotifyHealStartedClientRpc();
    }

    public void StopHealing()
    {
        if (!IsServer) return;
        isBeingHealed = false;
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
    }
}
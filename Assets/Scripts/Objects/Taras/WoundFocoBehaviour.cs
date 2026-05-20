using Unity.Netcode;
using UnityEngine;

public class WoundFocoBehaviour : TaraBase
{
    private float healTimer = 0f;
    public float healingTimeRequired = 3f;

    private NetworkVariable<bool> isBeingHealed = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isBeingHealed.OnValueChanged += (oldVal, newVal) =>
        {
            if (newVal == true)
            {
                EventBus.Publish(new OnWoundFocoHealStarted
                {
                    TaraID = NetworkObjectId
                });
            }
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
                GetComponent<NetworkObject>().Despawn();
                Destroy(gameObject);
            }
        }
        else
        {
            healTimer = 0f;
        }

        isBeingHealed.Value = false;
    }
    private void LateUpdate()
    {
        if (!IsServer) return;

        Color rayColor = isBeingHealed.Value ? Color.green : Color.red;
        Debug.DrawLine(transform.position,
                      transform.position + Vector3.up * 2f,
                      rayColor);
    }

    public void ReceiveHealTick()
    {
        if (!IsServer) return;
        isBeingHealed.Value = true;
    }
    [ClientRpc]
    public void NotifyHealEndedClientRpc()
    {
        EventBus.Publish(new OnWoundFocoHealEnded
        {
            TaraID = NetworkObjectId
        });
    }
}
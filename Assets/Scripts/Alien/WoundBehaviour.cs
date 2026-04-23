using Unity.Netcode;
using UnityEngine;

public class WoundBehaviour : TaraBase
{
    private float healTimer = 0f;
    public float healingTimeRequired = 3f;

    private NetworkVariable<bool> isBeingHealed = new NetworkVariable<bool>(false);

    void Update()
    {
        if (!IsServer) return;

        if (isBeingHealed.Value)
        {
            healTimer += Time.deltaTime;
            if (healTimer >= healingTimeRequired)
            {
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

    public void ReceiveHealTick()
    {
        if (!IsServer) return;
        isBeingHealed.Value = true;
    }
}
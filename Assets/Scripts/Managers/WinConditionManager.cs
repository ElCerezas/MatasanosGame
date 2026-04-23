using Unity.Netcode;
using UnityEngine;

public class WinConditionManager : NetworkBehaviour
{
    private int tarasHealedCount = 0;
    private int activeTarasCount = 0;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        EventBus.Subscribe<TaraGeneratedEvent>(OnTaraSpawned);
        EventBus.Subscribe<TaraHealedEvent>(OnTaraHealed);
    }

    public override void OnNetworkDespawn()
    {
        EventBus.Unsubscribe<TaraGeneratedEvent>(OnTaraSpawned);
        EventBus.Unsubscribe<TaraHealedEvent>(OnTaraHealed);
    }

    private void OnTaraSpawned(TaraGeneratedEvent e)
    {
        activeTarasCount++;
    }

    private void OnTaraHealed(TaraHealedEvent e)
    {
        activeTarasCount--;
        tarasHealedCount++;

        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        if (activeTarasCount == 0 && tarasHealedCount > 0)
        {
            NotifyVictoryClientRpc();
        }
    }

    [ClientRpc]
    private void NotifyVictoryClientRpc()
    {
        EventBus.Publish(new VictoryEvent());
    }
}
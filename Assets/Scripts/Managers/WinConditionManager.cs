// WinConditionManager.cs
using Unity.Netcode;
using UnityEngine;

public class WinConditionManager : NetworkBehaviour
{
    [SerializeField] private int tarasHealedCount = 0;
    [SerializeField] private int activeTarasCount = 0;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        EventBus.Subscribe<TaraHealedEvent>(OnTaraHealed);

        StartCoroutine(CountTarasNextFrame());
    }

    private System.Collections.IEnumerator CountTarasNextFrame()
    {
        yield return null;
        foreach (var tara in FindObjectsByType<TaraBase>(FindObjectsSortMode.None))
        {
            if (tara.IsSpawned && !tara.WasHealed)
                activeTarasCount++;
        }
        //Debug.Log($"activeTarasCount inicial: {activeTarasCount}");
        CheckWinCondition();
    }
    private void OnTaraHealed(TaraHealedEvent e)
    {
        activeTarasCount--;
        tarasHealedCount++;
        //Debug.Log($"Tara healed, activeTarasCount: {activeTarasCount}, tarasHealedCount: {tarasHealedCount}");
        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        if (activeTarasCount == 0 && tarasHealedCount > 0)
            NotifyVictoryClientRpc();
    }

    [ClientRpc]
    private void NotifyVictoryClientRpc()
    {
        Debug.Log("NotifyVictoryClientRpc received!");
        EventBus.Publish(new VictoryEvent());
    }
}
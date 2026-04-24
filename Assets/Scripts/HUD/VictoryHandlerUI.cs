using System;
using Unity.Netcode;
using UnityEngine;

public class VictoryHandlerUI : NetworkBehaviour
{
    [SerializeField] GameObject victoryScreen;
    public override void OnNetworkSpawn()
    {
        EventBus.Subscribe<VictoryEvent>(OnVictory);
        victoryScreen.SetActive(false);
    }

    private void OnVictory(VictoryEvent @event)
    {
        victoryScreen.SetActive(true);
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

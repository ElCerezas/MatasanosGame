using System;
using Unity.Netcode;
using UnityEngine;

public class VictoryHandlerUI : MonoBehaviour
{
    [SerializeField] GameObject defeatScreen;
    [SerializeField] GameObject victoryScreen;
    public void Awake()
    {
        EventBus.Subscribe<VictoryEvent>(OnVictory);
        EventBus.Subscribe<AlienDeath>(OnAlienDeath);
        victoryScreen.SetActive(false);
        defeatScreen.SetActive(false);
    }

    private void OnVictory(VictoryEvent e)
    {
        Debug.Log("VictoryEvent received!");
        victoryScreen.SetActive(true);
    }
    
    private void OnAlienDeath(AlienDeath e)
    {
        Debug.Log("Defeat received!");
        defeatScreen.SetActive(true);
    }
}

using System;
using Unity.Netcode;
using UnityEngine;

public class VictoryHandlerUI : MonoBehaviour
{
    [SerializeField] GameObject victoryScreen;
    public void Awake()
    {
        EventBus.Subscribe<VictoryEvent>(OnVictory);
        victoryScreen.SetActive(false);
    }

    private void OnVictory(VictoryEvent e)
    {
        Debug.Log("VictoryEvent received!");
        //victoryScreen.SetActive(true);
    }
}

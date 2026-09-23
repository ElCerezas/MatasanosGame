using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryHandlerUI : MonoBehaviour
{
    [SerializeField] GameObject defeatScreen;
    [SerializeField] GameObject victoryScreen;
    bool canChangeScene = false;
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
        canChangeScene = true;
    }
    
    private void OnAlienDeath(AlienDeath e)
    {
        Debug.Log("Defeat received!");
        defeatScreen.SetActive(true);
        canChangeScene = true;
    }

    public void Update()
    {
        if (canChangeScene && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("changing scene to main menu");
            SceneManager.LoadScene("MainMenu");
        };
    }
    private void OnDisable()
    {
        EventBus.Unsubscribe<AlienDeath>(OnAlienDeath);
        EventBus.Unsubscribe<VictoryEvent>(OnVictory); 
    }
}

using UnityEngine;

public class VictoryHandlerUI : MonoBehaviour
{
    [SerializeField] GameObject defeatScreen;
    [SerializeField] GameObject victoryScreen;

    private void OnEnable()
    {
        EventBus.Subscribe<VictoryEvent>(OnVictory);
        EventBus.Subscribe<AlienDeath>(OnAlienDeath);
    }

    private void Start()
    {
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

    private void OnDisable()
    {
        EventBus.Unsubscribe<AlienDeath>(OnAlienDeath);
        EventBus.Unsubscribe<VictoryEvent>(OnVictory);
    }
}
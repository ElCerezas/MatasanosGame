using UnityEngine;

public class VictoryHandlerUI : MonoBehaviour
{
    [SerializeField] GameObject defeatScreen;
    [SerializeField] GameObject victoryScreen;

    private void Awake()
    {
        EventBus.Subscribe<VictoryEvent>(OnVictory);
        EventBus.Subscribe<AlienDeath>(OnAlienDeath);

        if (victoryScreen != null) victoryScreen.SetActive(false);
        if (defeatScreen != null) defeatScreen.SetActive(false);
    }

    private void OnDestroy()
    {
        // Usar OnDestroy evita que se desuscriba si el panel se oculta (SetActive false)
        EventBus.Unsubscribe<AlienDeath>(OnAlienDeath);
        EventBus.Unsubscribe<VictoryEvent>(OnVictory);
    }

    private void OnVictory(VictoryEvent e)
    {
        Debug.Log("Victoria recibida en cliente/host");
        if (victoryScreen != null) victoryScreen.SetActive(true);
    }

    private void OnAlienDeath(AlienDeath e)
    {
        Debug.Log("Derrota recibida en cliente/host");
        if (defeatScreen != null) defeatScreen.SetActive(true);
    }
}
using Unity.Netcode;
using UnityEngine;

public class MenuNetwork : MonoBehaviour
{
    [SerializeField] private string m_GameSceneName = "PlaygroundScene";
    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    private void OnServerStarted()
    {
        NetworkManager.Singleton.SceneManager.LoadScene(m_GameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    } 
}

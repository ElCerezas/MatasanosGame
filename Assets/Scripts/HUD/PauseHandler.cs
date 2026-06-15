using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PauseHandler : NetworkBehaviour
{
    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject menuVisual;
    [SerializeField] RawImage pauseRawImage;
    [SerializeField] List<Texture2D> pauseMenuTextures;
    [SerializeField] private FMODUnity.EventReference pauseSound;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private NetworkVariable<bool> isPlayerPaused = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> selectedTextureIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public bool IsPaused => isPlayerPaused.Value;

    public override void OnNetworkSpawn()
    {
        isPlayerPaused.OnValueChanged += OnPauseStateChanged;
        selectedTextureIndex.OnValueChanged += OnTextureChanged;

        if (IsOwner)
        {
            if (pauseMenu != null) pauseMenu.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            if (pauseMenu != null) pauseMenu.SetActive(false);
            if (menuVisual != null) menuVisual.SetActive(isPlayerPaused.Value);
        }

        if (selectedTextureIndex.Value >= 0 && selectedTextureIndex.Value < pauseMenuTextures.Count)
        {
            pauseRawImage.texture = pauseMenuTextures[selectedTextureIndex.Value];
        }
    }

    public override void OnNetworkDespawn()
    {
        isPlayerPaused.OnValueChanged -= OnPauseStateChanged;
        selectedTextureIndex.OnValueChanged -= OnTextureChanged;
    }

    public async void OnDisconnect()
    {
        if (!IsOwner) return;

        if (IsServer)
        {
            // Avisar a los clientes que vuelvan al menú principal
            KickClientsClientRpc();
            // Dar un pequeño margen de tiempo para que el mensaje viaje por la red antes de apagar
            await System.Threading.Tasks.Task.Delay(150);
        }

        await NetworkDisconnectHandler.ReturnToMainMenu(mainMenuSceneName);
    }

    [ClientRpc]
    private void KickClientsClientRpc()
    {
        if (IsServer) return;
        Debug.Log("[PauseHandler] Host ha cerrado");

        _ = NetworkDisconnectHandler.ReturnToMainMenu(mainMenuSceneName);
    }

    public void TogglePause()
    {
        if (!IsOwner) return;
        bool newPauseState = !isPlayerPaused.Value;
        isPlayerPaused.Value = newPauseState;

        if (newPauseState)
        {
            AudioManager.instance.PlayOneShot(pauseSound);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            GetComponent<PlayerController>().OnGamePaused(true);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            GetComponent<PlayerController>().OnGamePaused(false);
        }

        if (isPlayerPaused.Value && pauseMenuTextures.Count > 0)
        {
            selectedTextureIndex.Value = Random.Range(0, pauseMenuTextures.Count);
        }
        PauseManager.Instance?.SetPaused(isPlayerPaused.Value);
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(isPlayerPaused.Value);
        }
    }

    private void OnTextureChanged(int oldValue, int newValue)
    {
        if (newValue >= 0 && newValue < pauseMenuTextures.Count)
        {
            pauseRawImage.texture = pauseMenuTextures[newValue];
        }
    }

    private void OnPauseStateChanged(bool previousValue, bool newValue)
    {
        if (!IsOwner && menuVisual != null) { menuVisual.SetActive(newValue); }
    }
}
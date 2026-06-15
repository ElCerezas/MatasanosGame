using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;
public class UIMultiplayerSessionManager : MonoBehaviour
{
    [Header("Crear sesion")]
    [SerializeField] TMP_InputField sessionNameInput;
    [SerializeField] Button createSessionButton;

    [Header("Lista de sesiones")]
    [SerializeField] ScrollRect sessionsScrollRect;
    [SerializeField] Transform sessionListContent;
    [SerializeField] GameObject sessionItemPrefab;
    [SerializeField] Button refreshButton;

    [Header("Configuracion")]
    [SerializeField] int maxPlayers = 4;

    ISession activeSession;

    private bool isOperationInProgress;

    async void Start()
    {
        createSessionButton.onClick.AddListener(OnCreateSessionButtonClicked);
        refreshButton.onClick.AddListener(OnRefreshButtonClicked);

        await InitializeAndAuthenticateAsync();
        await RefreshSessionListAsync();
    }

    void OnDestroy()
    {
        if (createSessionButton != null)
            createSessionButton.onClick.RemoveListener(OnCreateSessionButtonClicked);

        if (refreshButton != null)
            refreshButton.onClick.RemoveListener(OnRefreshButtonClicked);
    }

    async Task InitializeAndAuthenticateAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch { }
    }

    async void OnCreateSessionButtonClicked()
    {
        if (isOperationInProgress) return;

        string sessionName = sessionNameInput != null ? sessionNameInput.text.Trim() : string.Empty;

        if (string.IsNullOrEmpty(sessionName)) return;

        SetBusy(true);

        try
        {
            var options = new SessionOptions
            { Name = sessionName, MaxPlayers = maxPlayers }.WithRelayNetwork();

            activeSession = await MultiplayerService.Instance.CreateSessionAsync(options);
        }
        finally
        {
            SetBusy(false);
        }
    }

    async void OnRefreshButtonClicked()
    {
        if (isOperationInProgress) return;

        await RefreshSessionListAsync();
    }

    async Task RefreshSessionListAsync()
    {
        SetBusy(true);
        ClearSessionList();

        try
        {
            var queryOptions = new QuerySessionsOptions();

            QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(queryOptions);
            foreach (ISessionInfo sessionInfo in results.Sessions)
            {
                SpawnSessionListItem(sessionInfo);
            }

            if (sessionsScrollRect != null)
            {
                sessionsScrollRect.verticalNormalizedPosition = 1f;
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    void ClearSessionList()
    {
        if (sessionListContent == null)
            return;

        for (int i = sessionListContent.childCount - 1; i >= 0; i--)
            Destroy(sessionListContent.GetChild(i).gameObject);
    }

    void SpawnSessionListItem(ISessionInfo sessionInfo)
    {
        if (sessionItemPrefab == null || sessionListContent == null)
            return;

        GameObject itemInstance = Instantiate(sessionItemPrefab, sessionListContent);

        TMP_Text sessionNameText = itemInstance.GetComponentInChildren<TMP_Text>();
        if (sessionNameText != null)
        {
            string displayName = string.IsNullOrEmpty(sessionInfo.Name) ? sessionInfo.Id : sessionInfo.Name;
            sessionNameText.text = displayName;
        }

        Button joinButton = itemInstance.GetComponentInChildren<Button>();
        if (joinButton != null)
        {
            string sessionId = sessionInfo.Id;
            joinButton.onClick.AddListener(() => OnJoinSessionButtonClicked(sessionId));
        }
    }

    async void OnJoinSessionButtonClicked(string sessionId)
    {
        if (isOperationInProgress)
            return;

        if (string.IsNullOrEmpty(sessionId)) return;

        SetBusy(true);
        try
        {
            activeSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
        }
        finally
        {
            SetBusy(false);
        }
    }

    void SetBusy(bool busy)
    {
        isOperationInProgress = busy;

        if (createSessionButton != null) createSessionButton.interactable = !busy;
        if (refreshButton != null) refreshButton.interactable = !busy;
        if (sessionNameInput != null) sessionNameInput.interactable = !busy;
    }
}
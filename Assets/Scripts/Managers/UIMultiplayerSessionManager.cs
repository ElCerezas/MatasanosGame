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
    [SerializeField] TMP_InputField sessionPasswordInput;
    [SerializeField] Button createSessionButton;

    [Header("Lista de sesiones")]
    [SerializeField] ScrollRect sessionsScrollRect;
    [SerializeField] Transform sessionListContent;
    [SerializeField] GameObject sessionItemPrefab;
    [SerializeField] RectTransform Content;
    [SerializeField] Button refreshButton;

    [Header("Pantalla Flotante Contraseña")]
    [SerializeField] GameObject passwordPopupPanel;
    [SerializeField] TMP_InputField popupPasswordInput;
    [SerializeField] Button popupConfirmJoinButton;
    [SerializeField] Button popupCancelButton;

    [Header("Configuracion")]
    [SerializeField] int maxPlayers = 4;

    ISession activeSession;
    private bool isOperationInProgress;
    private string pendingSessionId;

    async void Start()
    {
        createSessionButton.onClick.AddListener(OnCreateSessionButtonClicked);
        refreshButton.onClick.AddListener(OnRefreshButtonClicked);

        if (popupConfirmJoinButton != null) popupConfirmJoinButton.onClick.AddListener(OnPasswordConfirmClicked);
        if (popupCancelButton != null) popupCancelButton.onClick.AddListener(OnPasswordCancelClicked);

        if (passwordPopupPanel != null) passwordPopupPanel.SetActive(false);

        await InitializeAndAuthenticateAsync();
        await RefreshSessionListAsync();
    }
    void OnDestroy()
    {
        if (createSessionButton != null)
            createSessionButton.onClick.RemoveListener(OnCreateSessionButtonClicked);

        if (refreshButton != null)
            refreshButton.onClick.RemoveListener(OnRefreshButtonClicked);

        if (popupConfirmJoinButton != null) popupConfirmJoinButton.onClick.RemoveListener(OnPasswordConfirmClicked);
        if (popupCancelButton != null) popupCancelButton.onClick.RemoveListener(OnPasswordCancelClicked);
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
        string sessionPassword = sessionPasswordInput != null ? sessionPasswordInput.text.Trim() : string.Empty;
        Debug.Log(sessionPassword);
        if (string.IsNullOrEmpty(sessionName)) return;

        SetBusy(true);

        try
        {
            SessionOptions options;
            if (string.IsNullOrEmpty(sessionPassword))
                options = new SessionOptions { Name = sessionName, MaxPlayers = maxPlayers }.WithRelayNetwork();
            else
                options = new SessionOptions { Name = sessionName, MaxPlayers = maxPlayers, Password = sessionPassword }.WithRelayNetwork();

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
            int sessionsLoaded = 0;
            QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(queryOptions);
            foreach (ISessionInfo sessionInfo in results.Sessions)
            {
                SpawnSessionListItem(sessionInfo);
                sessionsLoaded++;
            }
            Content.offsetMin = new Vector2(Content.offsetMin.x, sessionsLoaded * 80f);
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
            sessionNameText.text = $"{displayName} - {sessionInfo.MaxPlayers - sessionInfo.AvailableSlots}/{sessionInfo.MaxPlayers}";
        }

        Button joinButton = itemInstance.GetComponentInChildren<Button>();
        if (joinButton != null)
        {
            TMP_Text joinButtonText = joinButton.GetComponentInChildren<TMP_Text>();
            Image joinButtonImage = joinButton.GetComponent<Image>();
            string sessionId = sessionInfo.Id;

            joinButton.onClick.RemoveAllListeners();

            /*if (IsGameStarted(sessionInfo))
            {
                if (joinButtonText != null) joinButtonText.text = "Game started";
                if (joinButtonImage != null) joinButtonImage.color = Color.gray;
                joinButton.interactable = false;
            }*/
            /*else*/ if (sessionInfo.AvailableSlots == 0)
            {
                if (joinButtonText != null) joinButtonText.text = "Session Full";
                if (joinButtonImage != null) joinButtonImage.color = Color.red;
                joinButton.interactable = false;
            }
            else if (sessionInfo.HasPassword)
            {
                if (joinButtonText != null) joinButtonText.text = "Enter Password";
                if (joinButtonImage != null) joinButtonImage.color = Color.yellow;
                joinButton.interactable = true;
                joinButton.onClick.AddListener(() => OpenPasswordPopup(sessionId));
            }
            else
            {
                if (joinButtonText != null) joinButtonText.text = "Join Session";
                if (joinButtonImage != null) joinButtonImage.color = Color.blue;
                joinButton.interactable = true;
                joinButton.onClick.AddListener(() => OnJoinSessionButtonClicked(sessionId));
            }
        }
    }
    bool IsGameStarted(ISessionInfo sessionInfo)
    {
        if (sessionInfo.Properties != null && sessionInfo.Properties.TryGetValue("GameStarted", out var property))
        {
            return property.Value == "true";
        }
        return false;
    }
    void OpenPasswordPopup(string sessionId)
    {
        pendingSessionId = sessionId;
        if (popupPasswordInput != null) popupPasswordInput.text = string.Empty;
        if (passwordPopupPanel != null) passwordPopupPanel.SetActive(true);
    }
    void OnPasswordConfirmClicked()
    {
        string password = popupPasswordInput != null ? popupPasswordInput.text.Trim() : string.Empty;

        if (passwordPopupPanel != null) passwordPopupPanel.SetActive(false);

        if (!string.IsNullOrEmpty(pendingSessionId))
        {
            OnJoinSessionButtonClicked(pendingSessionId, password);
        }
    }
    void OnPasswordCancelClicked()
    {
        if (passwordPopupPanel != null) passwordPopupPanel.SetActive(false);
        pendingSessionId = null;
    }
    async void OnJoinSessionButtonClicked(string sessionId, string password = null)
    {
        if (isOperationInProgress)  return;
        if (string.IsNullOrEmpty(sessionId)) return;

        SetBusy(true);
        try
        {
            if (password != null) 
                activeSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
            else
            {
                var joinOptions = new JoinSessionOptions { Password = password };
                activeSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId, joinOptions);
            }
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
        if (sessionPasswordInput != null) sessionPasswordInput.interactable = !busy;
    }
}
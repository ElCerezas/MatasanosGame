using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class UIMultiplayerSessionManager : MonoBehaviour
{
    public enum ConnectionMode { Auto, OnlineOnly, LanOnly }

    public static ISession CurrentSession { get; private set; }
    public static bool IsLanMode { get; private set; }
    public static string LanSessionName { get; private set; }

    public static void SetLanGameStarted(bool started) { lanGameStarted = started; }

    static bool lanGameStarted;
    static string lanHostPassword = string.Empty;
    static int lanMaxPlayers = 4;

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

    [Header("Feedback de Estado")]
    [SerializeField] TMP_Text feedbackText;

    [Header("Configuracion")]
    [SerializeField] int maxPlayers = 4;

    [Header("Modo LAN")]
    [SerializeField] ConnectionMode connectionMode = ConnectionMode.Auto;
    [SerializeField] float onlineTimeoutSeconds = 6f;
    [SerializeField] ushort lanGamePort = 7777;
    [SerializeField] int lanDiscoveryPort = 47777;
    [SerializeField] string lanMagic = "MatasanosGame";

    ISession activeSession;
    private bool isOperationInProgress;
    private string pendingSessionId;

    [Serializable]
    class LanAnnouncement
    {
        public string magic;
        public string name;
        public int players;
        public int max;
        public bool pass;
        public bool started;
        public int port;
    }

    class LanEntry
    {
        public string ip;
        public LanAnnouncement data;
        public float lastSeen;
    }

    struct SessionEntry
    {
        public string Id;
        public string Name;
        public int MaxPlayers;
        public int AvailableSlots;
        public bool HasPassword;
        public bool GameStarted;
    }

    readonly Dictionary<string, LanEntry> lanSessions = new Dictionary<string, LanEntry>();
    UdpClient lanListener;
    UdpClient lanBroadcaster;
    List<IPAddress> lanBroadcastTargets;
    float nextBroadcastTime;
    bool lanJoinCallbacksSubscribed;

    async void Start()
    {
        createSessionButton.onClick.AddListener(OnCreateSessionButtonClicked);
        refreshButton.onClick.AddListener(OnRefreshButtonClicked);

        if (popupConfirmJoinButton != null) popupConfirmJoinButton.onClick.AddListener(OnPasswordConfirmClicked);
        if (popupCancelButton != null) popupCancelButton.onClick.AddListener(OnPasswordCancelClicked);

        if (passwordPopupPanel != null) passwordPopupPanel.SetActive(false);

        SetBusy(true);
        await InitializeConnectionAsync();
        SetBusy(false);

        await RefreshSessionListAsync();
    }

    void OnDestroy()
    {
        if (createSessionButton != null) createSessionButton.onClick.RemoveListener(OnCreateSessionButtonClicked);
        if (refreshButton != null) refreshButton.onClick.RemoveListener(OnRefreshButtonClicked);
        if (popupConfirmJoinButton != null) popupConfirmJoinButton.onClick.RemoveListener(OnPasswordConfirmClicked);
        if (popupCancelButton != null) popupCancelButton.onClick.RemoveListener(OnPasswordCancelClicked);

        UnsubscribeLanJoinCallbacks();
        CloseLanSockets();
    }

    async Task InitializeConnectionAsync()
    {
        IsLanMode = connectionMode == ConnectionMode.LanOnly;

        if (!IsLanMode)
        {
            bool online = await TryInitializeOnlineAsync();
            if (!online)
            {
                if (connectionMode == ConnectionMode.OnlineOnly)
                    ShowFeedback("No se pudo conectar con Unity Services.");
                else
                    IsLanMode = true;
            }
        }

        if (IsLanMode)
        {
            StartLanListener();
            if (feedbackText != null) feedbackText.text = "Modo LAN (sin internet)";
        }

    }
    async Task<bool> TryInitializeOnlineAsync()
    {
        try
        {
            Task work = InitializeAndAuthenticateAsync();
            Task finished = await Task.WhenAny(work, Task.Delay(TimeSpan.FromSeconds(onlineTimeoutSeconds)));
            if (finished != work)
            {
                Debug.LogWarning("Unity Services no respondio a tiempo: se usara el modo LAN.");
                return false;
            }

            await work; // relanza la excepcion si fallo
            return AuthenticationService.Instance.IsSignedIn;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Sin conexion a Unity Services ({ex.Message}): se usara el modo LAN.");
            return false;
        }
    }
    async Task InitializeAndAuthenticateAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    async void OnCreateSessionButtonClicked()
    {
        if (isOperationInProgress) return;

        string sessionName = sessionNameInput != null ? sessionNameInput.text.Trim() : string.Empty;
        string sessionPassword = sessionPasswordInput != null ? sessionPasswordInput.text.Trim() : string.Empty;

        if (string.IsNullOrEmpty(sessionName))
        {
            ShowFeedback("El nombre de la sesión no puede estar vacío.");
            return;
        }

        if (!string.IsNullOrEmpty(sessionPassword) && sessionPassword.Length < 8)
        {
            ShowFeedback("La contraseña debe tener al menos 8 caracteres.");
            return;
        }

        SetBusy(true);
        if (feedbackText != null) feedbackText.text = string.Empty;

        try
        {
            if (IsLanMode)
            {
                CreateLanSession(sessionName, sessionPassword);
            }
            else
            {
                SessionOptions options;
                if (string.IsNullOrEmpty(sessionPassword))
                    options = new SessionOptions { Name = sessionName, MaxPlayers = maxPlayers }.WithRelayNetwork();
                else
                    options = new SessionOptions { Name = sessionName, MaxPlayers = maxPlayers, Password = sessionPassword }.WithRelayNetwork();

                activeSession = await MultiplayerService.Instance.CreateSessionAsync(options);
                CurrentSession = activeSession;
            }
        }
        catch (Exception ex)
        {
            ShowFeedback($"Error al crear sesión: {ex.Message}");
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
        if (feedbackText != null && !IsLanMode) feedbackText.text = string.Empty;

        try
        {
            if (IsLanMode)
            {
                await RefreshLanListAsync();
            }
            else
            {
                var queryOptions = new QuerySessionsOptions();
                QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(queryOptions);
                foreach (ISessionInfo sessionInfo in results.Sessions)
                {
                    SpawnSessionListItem(new SessionEntry
                    {
                        Id = sessionInfo.Id,
                        Name = sessionInfo.Name,
                        MaxPlayers = sessionInfo.MaxPlayers,
                        AvailableSlots = sessionInfo.AvailableSlots,
                        HasPassword = sessionInfo.HasPassword,
                        GameStarted = IsGameStarted(sessionInfo)
                    });
                }
            }

            if (sessionsScrollRect != null)
                sessionsScrollRect.verticalNormalizedPosition = 1f;
        }
        catch (Exception ex)
        {
            ShowFeedback($"Error al cargar lista: {ex.Message}");
        }
        finally
        {
            if (this != null) SetBusy(false);
        }
    }

    void ClearSessionList()
    {
        if (sessionListContent == null) return;
        for (int i = sessionListContent.childCount - 1; i >= 0; i--)
            Destroy(sessionListContent.GetChild(i).gameObject);
    }

    void SpawnSessionListItem(SessionEntry sessionInfo)
    {
        if (sessionItemPrefab == null || sessionListContent == null) return;

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

            if (sessionInfo.GameStarted)
            {
                if (joinButtonText != null) joinButtonText.text = "Game started";
                if (joinButtonImage != null) joinButtonImage.color = Color.gray;
                joinButton.interactable = false;
            }
            else if (sessionInfo.AvailableSlots == 0)
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

    // =====================================================================
    //  POPUP CONTRASEÑA
    // =====================================================================

    void OpenPasswordPopup(string sessionId)
    {
        pendingSessionId = sessionId;
        if (popupPasswordInput != null) popupPasswordInput.text = string.Empty;
        if (passwordPopupPanel != null) passwordPopupPanel.SetActive(true);
    }

    void OnPasswordConfirmClicked()
    {
        string password = popupPasswordInput != null ? popupPasswordInput.text.Trim() : string.Empty;

        if (string.IsNullOrEmpty(password) || password.Length < 8)
        {
            ShowFeedback("La contraseña debe tener al menos 8 caracteres.");
            return;
        }

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

    // =====================================================================
    //  UNIRSE A SESION
    // =====================================================================

    async void OnJoinSessionButtonClicked(string sessionId, string password = null)
    {
        if (isOperationInProgress) return;
        if (string.IsNullOrEmpty(sessionId)) return;

        SetBusy(true);
        if (feedbackText != null) feedbackText.text = string.Empty;

        // En LAN la conexion es asincrona por callbacks: el "busy" se libera alli.
        if (IsLanMode)
        {
            try
            {
                JoinLanSession(sessionId, password);
            }
            catch (Exception ex)
            {
                ShowFeedback($"Error al unirse: {ex.Message}");
                SetBusy(false);
            }
            return;
        }

        try
        {
            if (string.IsNullOrEmpty(password))
            {
                activeSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
            }
            else
            {
                var joinOptions = new JoinSessionOptions { Password = password };
                activeSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId, joinOptions);
            }
            CurrentSession = activeSession;
        }
        catch (Exception ex)
        {
            ShowFeedback($"Error al unirse: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    // =====================================================================
    //  LAN: HOST
    // =====================================================================

    void CreateLanSession(string sessionName, string password)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) throw new Exception("No hay NetworkManager en la escena.");
        if (nm.IsListening) throw new Exception("Ya hay una partida en marcha.");

        var utp = nm.GetComponent<UnityTransport>();
        if (utp == null) throw new Exception("El NetworkManager no usa UnityTransport.");

        lanHostPassword = password ?? string.Empty;
        lanMaxPlayers = maxPlayers;
        lanGameStarted = false;
        LanSessionName = sessionName;

        // Escuchar en todas las interfaces de red.
        utp.SetConnectionData("127.0.0.1", lanGamePort, "0.0.0.0");

        nm.NetworkConfig.ConnectionApproval = true;
        nm.ConnectionApprovalCallback = LanApprovalCheck;

        if (!nm.StartHost())
            throw new Exception($"No se pudo iniciar el host (¿puerto {lanGamePort} ocupado?).");

        lanBroadcaster = new UdpClient { EnableBroadcast = true };
        lanBroadcastTargets = GetBroadcastTargets();
        nextBroadcastTime = 0f;

        if (feedbackText != null) feedbackText.text = $"Partida LAN creada: {sessionName}";
    }

    // Se ejecuta en el host por cada cliente que intenta conectarse.
    static void LanApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
                                 NetworkManager.ConnectionApprovalResponse response)
    {
        response.CreatePlayerObject = true;
        response.Pending = false;

        // El propio host siempre entra.
        if (request.ClientNetworkId == NetworkManager.ServerClientId)
        {
            response.Approved = true;
            return;
        }

        var nm = NetworkManager.Singleton;

        if (lanGameStarted)
        {
            response.Approved = false;
            response.Reason = "La partida ya ha empezado.";
            return;
        }

        if (nm != null && nm.ConnectedClientsIds.Count >= lanMaxPlayers)
        {
            response.Approved = false;
            response.Reason = "La sesión está llena.";
            return;
        }

        if (!string.IsNullOrEmpty(lanHostPassword))
        {
            string sent = (request.Payload != null && request.Payload.Length > 0)
                ? Encoding.UTF8.GetString(request.Payload)
                : string.Empty;

            if (sent != lanHostPassword)
            {
                response.Approved = false;
                response.Reason = "Contraseña incorrecta.";
                return;
            }
        }

        response.Approved = true;
    }

    // =====================================================================
    //  LAN: CLIENTE
    // =====================================================================

    void JoinLanSession(string sessionId, string password)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) throw new Exception("No hay NetworkManager en la escena.");
        if (nm.IsListening) throw new Exception("Ya hay una conexión activa.");

        if (!lanSessions.TryGetValue(sessionId, out LanEntry entry))
            throw new Exception("La partida ya no está disponible. Pulsa refrescar.");

        var utp = nm.GetComponent<UnityTransport>();
        if (utp == null) throw new Exception("El NetworkManager no usa UnityTransport.");

        utp.SetConnectionData(entry.ip, (ushort)(entry.data.port > 0 ? entry.data.port : lanGamePort));
        // Fallar rapido si el host no responde (por defecto puede tardar ~1 minuto).
        utp.ConnectTimeoutMS = 1000;
        utp.MaxConnectAttempts = 6;

        nm.NetworkConfig.ConnectionApproval = true;
        nm.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(password ?? string.Empty);

        LanSessionName = entry.data.name;

        SubscribeLanJoinCallbacks();
        if (!nm.StartClient())
        {
            UnsubscribeLanJoinCallbacks();
            throw new Exception("No se pudo iniciar el cliente.");
        }
    }

    void SubscribeLanJoinCallbacks()
    {
        if (lanJoinCallbacksSubscribed || NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback += OnLanClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnLanClientDisconnected;
        lanJoinCallbacksSubscribed = true;
    }

    void UnsubscribeLanJoinCallbacks()
    {
        if (!lanJoinCallbacksSubscribed) return;
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnLanClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnLanClientDisconnected;
        }
        lanJoinCallbacksSubscribed = false;
    }

    void OnLanClientConnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || clientId != nm.LocalClientId) return;

        UnsubscribeLanJoinCallbacks();
        SetBusy(false);
    }

    void OnLanClientDisconnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        string reason = nm != null ? nm.DisconnectReason : string.Empty;

        UnsubscribeLanJoinCallbacks();
        SetBusy(false);

        ShowFeedback(string.IsNullOrEmpty(reason)
            ? "No se pudo conectar con el host."
            : $"Error al unirse: {reason}");
    }

    // =====================================================================
    //  LAN: DESCUBRIMIENTO (broadcast UDP)
    // =====================================================================

    void Update()
    {
        if (!IsLanMode) return;
        PollLanListener();
        BroadcastIfHosting();
    }

    void StartLanListener()
    {
        if (lanListener != null) return;

        try
        {
            lanListener = new UdpClient(AddressFamily.InterNetwork);
            // Permite varias instancias en el mismo PC (util para testear).
            lanListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            lanListener.Client.Bind(new IPEndPoint(IPAddress.Any, lanDiscoveryPort));
            lanListener.EnableBroadcast = true;
        }
        catch (Exception ex)
        {
            lanListener = null;
            ShowFeedback($"No se pudo abrir la búsqueda LAN: {ex.Message}");
        }
    }

    void PollLanListener()
    {
        if (lanListener == null) return;

        try
        {
            while (lanListener.Available > 0)
            {
                IPEndPoint ep = null;
                byte[] bytes = lanListener.Receive(ref ep);

                LanAnnouncement data = JsonUtility.FromJson<LanAnnouncement>(Encoding.UTF8.GetString(bytes));
                if (data == null || data.magic != lanMagic) continue;

                string ip = ep.Address.ToString();
                lanSessions[ip] = new LanEntry { ip = ip, data = data, lastSeen = Time.unscaledTime };
            }
        }
        catch (Exception)
        {
            // Paquetes ajenos o mal formados: se ignoran.
        }
    }

    void BroadcastIfHosting()
    {
        if (lanBroadcaster == null) return;

        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer)
        {
            lanBroadcaster.Close();
            lanBroadcaster = null;
            return;
        }

        if (Time.unscaledTime < nextBroadcastTime) return;
        nextBroadcastTime = Time.unscaledTime + 1f;

        var announcement = new LanAnnouncement
        {
            magic = lanMagic,
            name = LanSessionName,
            players = nm.ConnectedClientsIds.Count,
            max = lanMaxPlayers,
            pass = !string.IsNullOrEmpty(lanHostPassword),
            started = lanGameStarted,
            port = lanGamePort
        };

        byte[] payload = Encoding.UTF8.GetBytes(JsonUtility.ToJson(announcement));

        foreach (IPAddress target in lanBroadcastTargets)
        {
            try { lanBroadcaster.Send(payload, payload.Length, new IPEndPoint(target, lanDiscoveryPort)); }
            catch (Exception) { /* una interfaz puede fallar: se prueba con las demas */ }
        }
    }

    async Task RefreshLanListAsync()
    {
        StartLanListener();
        lanSessions.Clear();

        // Los hosts emiten cada 1 s: esperamos a escuchar al menos un anuncio.
        await Task.Delay(1500);
        if (this == null) return;

        foreach (LanEntry e in lanSessions.Values)
        {
            if (Time.unscaledTime - e.lastSeen > 4f) continue;

            SpawnSessionListItem(new SessionEntry
            {
                Id = e.ip,
                Name = e.data.name,
                MaxPlayers = e.data.max,
                AvailableSlots = Mathf.Max(0, e.data.max - e.data.players),
                HasPassword = e.data.pass,
                GameStarted = e.data.started
            });
        }

        if (lanSessions.Count == 0 && feedbackText != null)
            feedbackText.text = "Modo LAN: no se han encontrado partidas.";
    }

    // Direcciones de broadcast de cada interfaz activa + 255.255.255.255
    static List<IPAddress> GetBroadcastTargets()
    {
        var targets = new List<IPAddress> { IPAddress.Broadcast };

        try
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                foreach (UnicastIPAddressInformation ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork || ua.IPv4Mask == null) continue;

                    byte[] ip = ua.Address.GetAddressBytes();
                    byte[] mask = ua.IPv4Mask.GetAddressBytes();
                    var b = new byte[4];
                    for (int i = 0; i < 4; i++) b[i] = (byte)(ip[i] | ~mask[i]);

                    var bcast = new IPAddress(b);
                    if (!targets.Contains(bcast)) targets.Add(bcast);
                }
            }
        }
        catch (Exception)
        {
            // En algunas plataformas no se pueden listar interfaces: queda 255.255.255.255.
        }

        return targets;
    }

    void CloseLanSockets()
    {
        try { lanListener?.Close(); } catch (Exception) { }
        try { lanBroadcaster?.Close(); } catch (Exception) { }
        lanListener = null;
        lanBroadcaster = null;
    }

    // =====================================================================
    //  UI
    // =====================================================================

    void SetBusy(bool busy)
    {
        isOperationInProgress = busy;

        if (createSessionButton != null) createSessionButton.interactable = !busy;
        if (refreshButton != null) refreshButton.interactable = !busy;
        if (sessionNameInput != null) sessionNameInput.interactable = !busy;
        if (sessionPasswordInput != null) sessionPasswordInput.interactable = !busy;
    }

    void ShowFeedback(string message)
    {
        if (feedbackText != null) feedbackText.text = message;
        Debug.LogError(message);
    }
}